using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using AllStarsRacingLib.IO;

namespace AllStarsRacingLib
{
    /// <summary>
    /// Represents a packed binary containing hashed and compressed read-only file data.
    /// </summary>
    public class PackFile : IDisposable
    {
        internal static IReadOnlyDictionary<uint, string> FileHashToNameMap;

        static PackFile()
        {
            FileHashToNameMap = ParseFileHashToNameMapFile();
        }

        private static Dictionary<uint, string> ParseFileHashToNameMapFile()
        {
            var map = new Dictionary<uint, string>();

            using ( var reader = File.OpenText( @"D:\Games\PC\SteamLibrary\steamapps\common\Sonic and SEGA All Stars Racing\HashMap.txt" ) )
            {
                while ( !reader.EndOfStream )
                {
                    var line = reader.ReadLine();
                    var lineSplit = line.Split( '\t' );

                    uint hash = uint.Parse( lineSplit[0], System.Globalization.NumberStyles.HexNumber );
                    var name = lineSplit[1];

                    map[hash] = name;
                }
            }

            return map;
        }

        private Stream mStream;
        private Dictionary<uint, PackFileEntry> mFileEntryByHash;

        /// <summary>
        /// Gets the data alignment.
        /// </summary>
        public int Alignment { get; private set; }

        /// <summary>
        /// Gets the number of files in the pack file.
        /// </summary>
        public int FileCount { get; private set; }

        /// <summary>
        /// Constructs a new packfile from a given binary pack file.
        /// </summary>
        /// <param name="path">Path to the binary pack file.</param>
        public PackFile( string path )
        {
            ReadFromStream( File.OpenRead( path ) );
        }

        /// <summary>
        /// Constructs a new packfile from a given binary pack file stream.
        /// </summary>
        /// <param name="stream">Stream of the binary pack file.</param>
        public PackFile( Stream stream )
        {
            ReadFromStream( stream );
        }

        /// <summary>
        /// Opens a file for reading.
        /// </summary>
        /// <param name="name">Name of the file.</param>
        /// <returns>Readable stream of the file's data.</returns>
        public Stream OpenFile( string name )
        {
            if ( !TryOpenFile( name, out var stream ) )
            {
                throw new FileHashNotFoundException( $"No entry with name \"{name}\" found in pack file" );
            }

            return stream;
        }

        /// <summary>
        /// Opens a file for reading.
        /// </summary>
        /// <param name="hash">Hash of the file name.</param>
        /// <returns>Readable stream of the file's data.</returns>
        public Stream OpenFile( uint hash )
        {
            if ( !TryOpenFile( hash, out var stream ))
            {
                throw new FileHashNotFoundException( $"No entry with hash {hash:X8} found in pack file" );
            }

            return stream;
        }

        /// <summary>
        /// Gets the file entry for a given file.
        /// </summary>
        /// <param name="name">Name of the file.</param>
        /// <returns>The file entry.</returns>
        public PackFileEntry GetFileEntry( string name )
        {
            if ( !TryGetFileEntry( name, out var entry ) )
            {
                throw new FileHashNotFoundException( $"No entry with name \"{name}\" found in pack file" );
            }

            return entry;
        }

        /// <summary>
        /// Gets the file entry for a given file.
        /// </summary>
        /// <param name="name">Hash of the file name.</param>
        /// <returns>The file entry.</returns>
        public PackFileEntry GetFileEntry( uint hash )
        {
            if ( !TryGetFileEntry( hash, out var entry ))
            {
                throw new FileHashNotFoundException( $"No entry with hash {hash:X8} found in pack file" );
            }

            return entry;
        }

        /// <summary>
        /// Tries to open a file for reading.
        /// </summary>
        /// <param name="name">Name of the file.</param>
        /// <param name="stream">Readable stream of the file's data.</param>
        /// <returns>Whether the operation succeeded or not.</returns>
        public bool TryOpenFile( string name, out Stream stream )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tries to open a file for reading.
        /// </summary>
        /// <param name="hash">Hash of the file.</param>
        /// <param name="stream">Readable stream of the file's data.</param>
        /// <returns>Whether the operation succeeded or not.</returns>
        public bool TryOpenFile( uint hash, out Stream stream )
        {
            if ( !TryGetFileEntry( hash, out var entry ) )
            {
                stream = null;
                return false;
            }

            stream = new StreamView( mStream, entry.Offset, entry.CompressedSize );
            var headerBytes = new byte[2];
            stream.Read( headerBytes, 0, 2 );

            if ( headerBytes[0] == 0x78 )
            {
                stream = new DeflateStream( stream, CompressionMode.Decompress );
            }
            else
            {
                stream.Seek( -2, SeekOrigin.Current ); 
            }

            return true;
        }

        /// <summary>
        /// Tries to get the file entry for a given file.
        /// </summary>
        /// <param name="name">Name of the file.</param>
        /// <param name="entry">The file entry.</param>
        /// <returns>Whether the operation succeeded or not.</returns>
        public bool TryGetFileEntry( string name, out PackFileEntry entry )
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Tries to get the file entry for a given file.
        /// </summary>
        /// <param name="hash">Hash of the file name.</param>
        /// <param name="entry">The file entry.</param>
        /// <returns>Whether the operation succeeded or not.</returns>
        public bool TryGetFileEntry( uint hash, out PackFileEntry entry )
        {
            if ( !mFileEntryByHash.TryGetValue( hash, out entry ) )
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Enumerates over all of the file entries in the pack file.
        /// </summary>
        /// <returns>Iterator over file entries.</returns>
        public IEnumerable<PackFileEntry> EnumerateFileEntries() => mFileEntryByHash.Values;

        private void ReadFromStream( Stream stream )
        {
            using ( var reader = new BinaryReader( stream, Encoding.Default, true ) )
            {
                ReadHeader( reader );
                ReadFileEntries( reader );
            }

            mStream = stream;
        }

        private void ReadHeader( BinaryReader reader )
        {
            int field00 = reader.ReadInt32();
            if ( field00 != 0 )
                throw new Exception( "PackFile header Field00 not 0" );

            int field04 = reader.ReadInt32();
            if ( field04 != 0 )
                throw new Exception( "PackFile header Field04 not 0" );

            Alignment = reader.ReadInt32();
            FileCount = reader.ReadInt32();

            int field0c = reader.ReadInt32();
            if ( field0c != 0 )
                throw new Exception( "PackFile header Field0c not 0" );
        }

        private void ReadFileEntries( BinaryReader reader )
        {
            mFileEntryByHash = new Dictionary<uint, PackFileEntry>( FileCount );

            for ( int i = 0; i < FileCount; i++ )
            {
                var entry = ReadFileEntry( reader );
                mFileEntryByHash[entry.Hash] = entry;
            }
        }

        private PackFileEntry ReadFileEntry( BinaryReader reader )
        {
            int field00 = reader.ReadInt32();
            if ( field00 != 0 )
                throw new Exception( "PackFileEntry Field00 != 0" );

            uint hash = reader.ReadUInt32();
            uint offset = reader.ReadUInt32();
            uint compressedSize = reader.ReadUInt32();
            uint uncompressedSize = reader.ReadUInt32();
            FileHashToNameMap.TryGetValue( hash, out var name );

            return new PackFileEntry( hash, name, offset, compressedSize, uncompressedSize );
        }

        public void Dispose()
        {
            ( ( IDisposable )mStream ).Dispose();
        }
    }
}
