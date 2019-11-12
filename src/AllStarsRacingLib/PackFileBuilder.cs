using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Text;
using AllStarsRacingLib.IO;

namespace AllStarsRacingLib
{
    public class PackFileBuilder
    {
        class InputFile
        {
            public string Path;
            public string VirtualPath;
            public Stream Stream;
            public bool EnableCompression;

            public InputFile( string path, string virtualPath, Stream stream, bool enableCompression )
            {
                Path = path;
                VirtualPath = virtualPath;
                Stream = stream;
                EnableCompression = enableCompression;
            }
        }

        private List<InputFile> mInputFiles;
        private int mAlignment;
        private Nullable<bool> mForceCompressionOnOrOf;
        private string mBaseDirectory;

        public PackFileBuilder( string baseDirectory )
        {
            mInputFiles = new List<InputFile>();
            mAlignment = 2048;
            mBaseDirectory = Path.GetFullPath( baseDirectory );
            if ( !mBaseDirectory.EndsWith( "\\" ) )
                mBaseDirectory += "\\";
        }

        public PackFileBuilder AddFile( string path, bool enableCompression = true )
        {
            mInputFiles.Add( new InputFile( Path.GetFullPath(path), NormalizeVirtualPath(path), null, enableCompression ) );

            return this;
        }

        public PackFileBuilder AddFile( Stream stream, string virtualPath, bool enableCompression = true )
        {
            mInputFiles.Add( new InputFile( null, NormalizeVirtualPath(virtualPath), stream, enableCompression ) );

            return this;
        }

        public PackFileBuilder AddDirectory( string path, bool includeDirectoryInPath, bool recurse, bool enableCompression = true )
        {
            return AddDirectory( path, includeDirectoryInPath, "*.*", recurse, enableCompression );
        }

        public PackFileBuilder AddDirectory( string path, bool includeDirectoryInPath, string searchPattern, bool recurse, bool enableCompression = true )
        {
            if ( !Directory.Exists( path ) )
                throw new DirectoryNotFoundException( path );

            foreach ( var filePath in Directory.EnumerateFiles( path, searchPattern, recurse ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly ) )
            {
                AddFile( filePath, enableCompression );
            }

            return this;
        }

        public void SetAlignment( int alignment )
        {
            mAlignment = alignment;
        }

        public void SetForceCompression( bool forceCompression )
        {
            mForceCompressionOnOrOf = forceCompression;
        }

        public PackFile Build()
        {
            var stream = BuildStream();
            return new PackFile( stream );
        }

        public void BuildFile( string path )
        {
            WriteToStream( File.Create( path ), false );
        }

        public Stream BuildStream()
        {
            var stream = new MemoryStream();
            WriteToStream( stream, true );
            return stream;
        }

        private string NormalizeVirtualPath( string path )
        {
            var fullPath = Path.GetFullPath( path );
            var virtualPath = PathHelper.MakeRelativePath( mBaseDirectory, fullPath );

            virtualPath = virtualPath.Replace( '/', '\\' );

            if ( !virtualPath.StartsWith( ".\\" ) )
                virtualPath = ".\\" + virtualPath;

            return virtualPath;
        }

        private void WriteToStream( Stream stream, bool leaveOpen )
        {
            using ( var writer = new BinaryWriter( stream, Encoding.Default, leaveOpen ) )
            {
                // header
                writer.Write( ( int )0 );
                writer.Write( ( int )0 );
                writer.Write( mAlignment );
                writer.Write( mInputFiles.Count );
                writer.Write( ( int )0 );

                uint offset = (uint)AlignmentHelper.Align( 0x14 + ( 0x14 * mInputFiles.Count ), mAlignment );

                foreach ( var file in mInputFiles )
                {
                    var fileStream = file.Stream;
                    if ( fileStream == null )
                    {
                        fileStream = File.OpenRead( file.Path );
                    }

                    byte[] buffer;
                    bool isCompressed = false;

                    if ( file.EnableCompression && !( mForceCompressionOnOrOf.HasValue && !mForceCompressionOnOrOf.Value ) )
                    {
                        using ( var deflateStream = new MemoryStream() )
                        using ( var deflator = new DeflateStream( deflateStream, CompressionMode.Compress, true ) )
                        {
                            fileStream.CopyTo( deflator );

                            if ( file.Stream == null )
                                fileStream.Close();
                            else
                                file.Stream.Position = 0;

                            deflator.Close();
                            buffer = deflateStream.ToArray();
                        }

                        isCompressed = true;
                    }
                    else
                    {
                        buffer = new byte[fileStream.Length];
                        fileStream.Read( buffer, 0, (int)fileStream.Length );
                    }

                    if ( !uint.TryParse( Path.GetFileNameWithoutExtension( file.VirtualPath ), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint hash ) )
                    {
                        hash = StringHasher.ComputeSimpleHash( file.VirtualPath );
                    }

                    writer.Write( ( int )0 );
                    writer.Write( hash );
                    writer.Write( offset );

                    if ( isCompressed )
                    {
                        writer.Write( buffer.Length + 2 );
                        writer.Write( buffer.Length + 2 );
                    }
                    else
                    {
                        writer.Write( buffer.Length );
                        writer.Write( buffer.Length );
                    }

                    long temp = writer.BaseStream.Position;
                    writer.BaseStream.Position = offset;

                    if ( isCompressed )
                    {
                        writer.Write( ( byte )0x78 );
                        writer.Write( ( byte )0xDA );
                    }

                    writer.Write( buffer );

                    int alignedDifference = AlignmentHelper.GetAlignedDifference( writer.BaseStream.Position, mAlignment );
                    for ( int i = 0; i < alignedDifference; i++ )
                        writer.Write( ( byte )0 );

                    offset = ( uint )writer.BaseStream.Position;

                    writer.BaseStream.Position = temp;
                }
            }
        }
    }
}
