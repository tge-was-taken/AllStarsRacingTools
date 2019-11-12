using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AllStarsRacingLib;

namespace AllStarsRacingPackFile
{
    class Program
    {
        static void Main( string[] args )
        {
            if ( args.Length < 1 )
                return;

            var path = Path.GetFullPath( args[0] );
            var basePath = Path.GetDirectoryName( path );
            var pathName = Path.GetFileNameWithoutExtension( path );
            var pathExtension = Path.GetExtension( path );

            if ( pathExtension.Equals(".xpac", StringComparison.InvariantCultureIgnoreCase ) )
            {
                var outputBasePath = Path.Combine( basePath, pathName );
                Directory.CreateDirectory( outputBasePath );

                // unpack xpac file
                using ( var packFile = new PackFile( path ) )
                {
                    foreach ( var entry in packFile.EnumerateFileEntries() )
                    {
                        var hasName = entry.Name != null;
                        var entryFileName = hasName ? entry.Name : entry.Hash.ToString( "X8" );
                        Console.WriteLine( $"Unpacking {entryFileName}" );

                        var entryFilePath = Path.Combine( outputBasePath, entryFileName );
                        var entryStream = packFile.OpenFile( entry.Hash );

                        byte[] fourcc = null;
                        if ( !hasName )
                        {
                            fourcc = new byte[16];
                            entryStream.Read( fourcc, 0, ( int )Math.Min( entry.UncompressedSize, 16 ) );

                            if ( fourcc.Length >= 4 && ( fourcc[0] == 'R' && fourcc[1] == 'I' && fourcc[2] == 'F' && fourcc[3] == 'F' ) )
                            {
                                if ( fourcc.Length >= 12 && ( fourcc[8] == 'X' && fourcc[9] == 'W' && fourcc[10] == 'M' && fourcc[11] == 'A' ) )
                                {
                                    entryFilePath = Path.ChangeExtension( entryFilePath, ".xwm" );
                                }
                                else
                                {
                                    entryFilePath = Path.ChangeExtension( entryFilePath, ".wav" );
                                }
                            }
                            else if ( fourcc.Length >= 5 && ( fourcc[0] == '<' && fourcc[1] == '?' && fourcc[2] == 'x' && fourcc[3] == 'm' && fourcc[4] == 'l' ) )
                            {
                                entryFilePath = Path.ChangeExtension( entryFilePath, ".xml" );
                            }
                            else if ( fourcc.Length >= 8 && ( fourcc[4] == 'F' || fourcc[5] == 'O' || fourcc[6] == 'R' || fourcc[7] == 'E' ) )
                            {
                                entryFilePath = Path.ChangeExtension( entryFilePath, ".forest" );
                            }
                            else if ( fourcc.All( x => char.IsLetterOrDigit( ( char )x ) ) )
                            {
                                entryFilePath = Path.ChangeExtension( entryFilePath, ".txt" );
                            }
                        }

                        Directory.CreateDirectory( Path.GetDirectoryName( entryFilePath ) );
                        using ( var entryFileStream = File.Create( entryFilePath ) )
                        {
                            if ( !hasName )
                                entryFileStream.Write( fourcc, 0, fourcc.Length );

                            entryStream.CopyTo( entryFileStream );
                        }
                    }
                }
            }
            else if ( Directory.Exists(path) )
            {
                // pack xpac from folder
                var packFileBuilder = new PackFileBuilder( @"D:\Games\PC\SteamLibrary\steamapps\common\Sonic and SEGA All Stars Racing\" );

                foreach ( var file in Directory.EnumerateFiles( path, "*.*", SearchOption.AllDirectories) )
                {
                    if ( Path.GetFileNameWithoutExtension( file ) != "2D529B4E" )
                        packFileBuilder.AddFile( file, true );
                    else
                        packFileBuilder.AddFile( file, false );

                    Console.WriteLine($"Added {file}.");
                }

                Console.WriteLine("Packing xpac...");
                packFileBuilder.BuildFile( Path.ChangeExtension( path, ".xpac" ) );
            }
        }
    }
}
