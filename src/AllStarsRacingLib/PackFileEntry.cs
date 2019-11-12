namespace AllStarsRacingLib
{
    /// <summary>
    /// Represents a single file entry in a pack file.
    /// </summary>
    public class PackFileEntry
    {
        /// <summary>
        /// Gets the hash of the file name.
        /// </summary>
        public uint Hash { get; }

        /// <summary>
        /// Gets the file name, if available. If it is not available, then this will return null.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the offset of the file data within the pack file.
        /// </summary>
        public uint Offset { get; }

        /// <summary>
        /// Gets the compressed size of the file's data.
        /// </summary>
        public uint CompressedSize { get; }

        /// <summary>
        /// Gets the uncompressed size of the file's data.
        /// </summary>
        public uint UncompressedSize { get; }

        internal PackFileEntry( uint hash, string name, uint offset, uint compressedSize, uint uncompressedSize )
        {
            Hash = hash;
            Name = name;
            Offset = offset;
            CompressedSize = compressedSize;
            UncompressedSize = uncompressedSize;
        }
    }
}
