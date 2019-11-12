using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using AllStarsRacingLib;

namespace AllStarsRacingTests
{
    class Program
    {
        static Stack<long> OffsetBase = new Stack<long>();

        static void Main( string[] args )
        {
            //Trace.Listeners.Add( new ConsoleTraceListener() );

            ForestFile forestFile;
            using ( var reader = new BinaryReader( File.OpenRead( @"D:\Games\PC\SteamLibrary\steamapps\common\Sonic and SEGA All Stars Racing\Resource\Select\Resource\Select\SonicSelect.zif" ) ) )
            {
                forestFile.FileSize = reader.ReadUInt32();
                forestFile.Chunks = new List<ForestChunk>();

                while ( reader.BaseStream.Position < reader.BaseStream.Length )
                {
                    OffsetBase.Push( reader.BaseStream.Position );
                    var header = ReadChunkHeader( reader );

                    Trace.WriteLine( $"Read ForestChunkHeader {header.FourCC} {header.ChunkSize:X8} {header.ContentSize:X8} {header.Field0C:X8} @ {reader.BaseStream.Position:X8}" );

                    var chunk = new ForestChunk();
                    chunk.Header = header;

                    OffsetBase.Push( reader.BaseStream.Position );
                    Trace.Indent();
                    switch ( header.FourCC )
                    {
                        case "FORE":
                            chunk.Content = ReadFOREChunk( reader );
                            break;

                        default:
                            break;
                    }
                    Trace.Unindent();

                    forestFile.Chunks.Add( chunk );
                    OffsetBase.Pop();

                    reader.BaseStream.Position = OffsetBase.Pop() + header.ChunkSize;
                }
            }

            using ( var writer = File.CreateText( "test.obj" ) )
            using ( var reader = new BinaryReader( File.OpenRead( @"D:\Games\PC\SteamLibrary\steamapps\common\Sonic and SEGA All Stars Racing\Resource\Select\Resource\Select\SonicSelect.zig" ) ) )
            {
                var forestList = ( ForestListHeader )forestFile.Chunks[0].Content;
                uint elementIndexOffset = 1;

                foreach ( var forest in forestList.Entries )
                {
                    foreach ( var model in forest.ForestHeader.Value.ContentType0Array.Value )
                    {
                        //writer.WriteLine( $"o {model.Value.Shape.Value.Name}" );

                        if ( model.Value.Shape.Value.Meshes.Value == null )
                            continue;

                        foreach ( var mesh in model.Value.Shape.Value.Meshes.Value )
                        {
                            long elementBufferOffset = mesh.Value.ElementBufferOffset + 4;
                            long vertexBufferOffset = mesh.Value.VertexBufferDescriptor.Value.VertexBufferOffset + 4;

                            writer.WriteLine();
                            writer.WriteLine( "#" );
                            writer.WriteLine( $"# Vertex Buffer @ {vertexBufferOffset:X8}" );
                            writer.WriteLine( "#" );
                            writer.WriteLine();
                            for ( int i = 0; i < mesh.Value.VertexBufferDescriptor.Value.VertexCount; i++ )
                            {
                                reader.BaseStream.Seek( vertexBufferOffset + ( i * mesh.Value.VertexBufferDescriptor.Value.VertexStride ), SeekOrigin.Begin );

                                /*
                                var px = ( float )reader.ReadInt16() / 0x4000;
                                var py = ( float )reader.ReadInt16() / 0x4000;
                                var pz = ( float )reader.ReadInt16() / 0x4000;
                                var pw = ( float )reader.ReadInt16() / 0x4000;

                                var nx = ( float )reader.ReadInt16() / 0x4000;
                                var ny = ( float )reader.ReadInt16() / 0x4000;
                                var nz = ( float )reader.ReadInt16() / 0x4000;
                                var nw = ( float )reader.ReadInt16() / 0x4000;

                                var u = ( float )reader.ReadInt16() / 0x4000;
                                var v = ( float )reader.ReadInt16() / 0x4000;

                                //var x = Half.ToHalf( reader.ReadUInt16() );
                                //var y = Half.ToHalf( reader.ReadUInt16() );
                                //var z = Half.ToHalf( reader.ReadUInt16() );
                                */

                                var px = reader.ReadSingle();
                                var py = reader.ReadSingle();
                                var pz = reader.ReadSingle();
                                var nx = 0;
                                var ny = 0;
                                var nz = 0;
                                var u = 0;
                                var v = 0;

                                writer.WriteLine( $"v {px} {py} {pz}" );
                                writer.WriteLine( $"vn {nx} {ny} {nz}" );
                                writer.WriteLine( $"vt {u} {v}" );
                            }
                            writer.WriteLine();
                            writer.WriteLine( "#" );
                            writer.WriteLine( $"# Element Buffer @ {elementBufferOffset:X8}" );
                            writer.WriteLine( "#" );
                            writer.WriteLine();
                            reader.BaseStream.Seek( elementBufferOffset, SeekOrigin.Begin );
                            for ( int i = 0; i < mesh.Value.ElementCount / 3; i++ )
                            {
                                var v1 = reader.ReadUInt16() + elementIndexOffset;
                                var v2 = reader.ReadUInt16() + elementIndexOffset;
                                var v3 = reader.ReadUInt16() + elementIndexOffset;
                                writer.WriteLine( $"f {v1}/{v1}/{v1} {v2}/{v2}/{v2} {v3}/{v3}/{v3}" );
                            }

                            return;
                            elementIndexOffset += mesh.Value.VertexBufferDescriptor.Value.VertexCount;
                        }
                    }
                }
            }
            /*
            while ( true )
            {
                Console.WriteLine( $"{StringHasher.ComputeSimpleHash( Console.ReadLine() ):X8}" );
            }
            */
        }

        private static ForestChunkHeader ReadChunkHeader( BinaryReader reader )
        {
            ForestChunkHeader header;
            header.FourCC = Encoding.ASCII.GetString( reader.ReadBytes( 4 ) );
            header.ChunkSize = reader.ReadUInt32();
            header.ContentSize = reader.ReadUInt32();
            header.Field0C = reader.ReadUInt32();

            return header;
        }

        private static string ReadStringAt( BinaryReader reader, uint offset )
        {
            reader.BaseStream.Seek( OffsetBase.Peek() + offset, SeekOrigin.Begin );
            var value = string.Empty;
            while ( true )
            {
                byte b = reader.ReadByte();
                if ( b != 0 )
                    value += ( char )b;
                else
                    break;
            }

            return value;
        }

        private static ForestListHeader ReadFOREChunk( BinaryReader reader )
        {
            ForestListHeader header;
            header.ForestCount = reader.ReadUInt32();
            header.Entries = new ForestEntryHeader[header.ForestCount];

            for ( int i = 0; i < header.Entries.Length; i++ )
            {
                Trace.WriteLine( $"Reading ForestEntry #{i:D2} @ {reader.BaseStream.Position:X8}" );
                Trace.Indent();
                header.Entries[i] = ReadForestEntry( reader );
                Trace.Unindent();
            }

            return header;
        }

        private static ForestEntryHeader ReadForestEntry( BinaryReader reader )
        {
            ForestEntryHeader header;
            header.Field00 = reader.ReadUInt32();
            header.Name.Offset = reader.ReadUInt32();
            header.ForestHeader.Offset = reader.ReadUInt32();
            header.Field0C = reader.ReadUInt32();

            long temp = reader.BaseStream.Position;

            header.Name.Value = ReadStringAt( reader, header.Name.Offset );

            reader.BaseStream.Seek( OffsetBase.Peek() + header.ForestHeader.Offset, SeekOrigin.Begin );
            Trace.WriteLine( $"Reading {header.Field00:X8} {header.Field0C:X8} {header.Name.Value} @ {reader.BaseStream.Position:X8}" );
            Trace.Indent();
            header.ForestHeader.Value = ReadForestHeader( reader );
            Trace.Unindent();

            reader.BaseStream.Position = temp;

            return header;
        }

        private static ForestHeader ReadForestHeader( BinaryReader reader )
        {
            OffsetBase.Push( reader.BaseStream.Position );

            ForestHeader header;
            header.ContentType0Count = reader.ReadUInt32();
            header.ContentType0Array.Offset = reader.ReadUInt32();
            header.TextureCount = reader.ReadUInt32();
            header.Textures.Offset = reader.ReadUInt32();
            header.ContentType2Count = reader.ReadUInt32();
            header.ContentType2Array.Offset = reader.ReadUInt32();
            header.ContentType3Count = reader.ReadUInt32();
            header.ContentType3ArrayOffset = reader.ReadUInt32();
            header.Field20 = reader.ReadUInt32();

            long temp = reader.BaseStream.Position;

            // content type 0
            reader.BaseStream.Seek( OffsetBase.Peek() + header.ContentType0Array.Offset, SeekOrigin.Begin );
            header.ContentType0Array.Value = new OffsetTo<ForestContentType0Header>[header.ContentType0Count];
            Trace.WriteLine( $"Reading {header.ContentType0Count} content 0 entries @ {reader.BaseStream.Position:X8}" );

            for ( int i = 0; i < header.ContentType0Array.Value.Length; i++ )
                header.ContentType0Array.Value[i].Offset = reader.ReadUInt32();

            Trace.Indent();
            for ( int i = 1; i < header.ContentType0Array.Value.Length; i++ )
            {
                reader.BaseStream.Seek( OffsetBase.Peek() + header.ContentType0Array.Value[i].Offset, SeekOrigin.Begin );
                Trace.WriteLine( $"Reading content 0 entry #{i:D2} @ {reader.BaseStream.Position:X8}" );
                Trace.Indent();
                header.ContentType0Array.Value[i].Value = ReadForestContentType0Header( reader );
                Trace.Unindent();
            }
            Trace.Unindent();

            // content type 1
            reader.BaseStream.Seek( OffsetBase.Peek() + header.Textures.Offset, SeekOrigin.Begin );
            header.Textures.Value = new OffsetTo<Texture>[header.TextureCount];
            Trace.WriteLine( $"Reading {header.TextureCount} texture entries @ {reader.BaseStream.Position:X8}" );

            for ( int i = 0; i < header.Textures.Value.Length; i++ )
                header.Textures.Value[i].Offset = reader.ReadUInt32();

            Trace.Indent();
            for ( int i = 0; i < header.Textures.Value.Length; i++ )
            {
                reader.BaseStream.Seek( OffsetBase.Peek() + header.Textures.Value[i].Offset, SeekOrigin.Begin );
                Trace.WriteLine( $"Reading texture entry #{i:D2} @ {reader.BaseStream.Position:X8}" );
                Trace.Indent();
                header.Textures.Value[i].Value = ReadForestTexture( reader );
                Trace.Unindent();
            }
            Trace.Unindent();

            // content type 2
            reader.BaseStream.Seek( OffsetBase.Peek() + header.ContentType2Array.Offset, SeekOrigin.Begin );
            header.ContentType2Array.Value = new ForestContentType2Header[header.ContentType2Count];
            Trace.WriteLine( $"Reading {header.ContentType2Count} content 2 entries @ {reader.BaseStream.Position:X8}" );

            Trace.Indent();
            for ( int i = 0; i < header.ContentType2Array.Value.Length; i++ )
            {
                Trace.WriteLine( $"Reading content 2 entry #{i:D2} @ {reader.BaseStream.Position:X8}" );
                Trace.Indent();
                header.ContentType2Array.Value[i] = ReadForestContentType2Header( reader );
                Trace.Unindent();
            }
            Trace.Unindent();

            reader.BaseStream.Position = temp;

            OffsetBase.Pop();
            return header;
        }

        private static ForestContentType0Header ReadForestContentType0Header( BinaryReader reader )
        {
            ForestContentType0Header header;
            header.Field00 = reader.ReadUInt32();
            header.Hash = reader.ReadUInt32();
            header.NodeCount = reader.ReadUInt32();
            header.Nodes.Offset = reader.ReadUInt32();
            header.Field10.Offset = reader.ReadUInt32();
            header.Field14.Offset = reader.ReadUInt32();
            header.Field18.Offset = reader.ReadUInt32();
            header.Field1C = reader.ReadUInt32();
            header.Field20 = reader.ReadUInt32();
            header.Field24 = reader.ReadUInt32();
            header.Field28 = reader.ReadUInt32();
            header.Field2C = reader.ReadUInt32();
            header.CameraCount = reader.ReadUInt32();
            header.Cameras.Offset = reader.ReadUInt32();
            header.Field38 = reader.ReadUInt32();
            header.Field3C = reader.ReadUInt32();
            header.Field40 = reader.ReadUInt32();
            header.Field44 = reader.ReadUInt32();
            header.Shape.Offset = reader.ReadUInt32();
            header.AnimationCount = reader.ReadUInt32();
            header.AnimationEntries.Offset = reader.ReadUInt32();
            header.Field54 = reader.ReadUInt32();
            header.Field58 = reader.ReadUInt32();
            header.Field5C = reader.ReadUInt32();
            header.Field60 = reader.ReadUInt32();

            Trace.WriteLine( $"Hash {header.Hash:X8}" );

            long temp = reader.BaseStream.Position;

            // nodes
            reader.BaseStream.Seek( OffsetBase.Peek() + header.Nodes.Offset, SeekOrigin.Begin );
            header.Nodes.Value = new OffsetTo<Node>[header.NodeCount];
            Trace.WriteLine( $"Reading {header.NodeCount} nodes @ {reader.BaseStream.Position:X8}" );

            for ( int i = 0; i < header.Nodes.Value.Length; i++ )
                header.Nodes.Value[i].Offset = reader.ReadUInt32();

            Trace.Indent();
            for ( int i = 0; i < header.Nodes.Value.Length; i++ )
            {
                reader.BaseStream.Seek( OffsetBase.Peek() + header.Nodes.Value[i].Offset, SeekOrigin.Begin );
                Trace.WriteLine( $"Reading node #{i:D2} @ {reader.BaseStream.Position:X8}" );
                Trace.Indent();
                header.Nodes.Value[i].Value = ReadNode( reader );
                Trace.Unindent();
            }
            Trace.Unindent();

            // color 1
            reader.BaseStream.Seek( OffsetBase.Peek() + header.Field10.Offset, SeekOrigin.Begin );
            header.Field10.Value = new float[20];
            for ( int i = 0; i < header.Field10.Value.Length; i++ )
                header.Field10.Value[i] = reader.ReadSingle();

            // color 2
            reader.BaseStream.Seek( OffsetBase.Peek() + header.Field14.Offset, SeekOrigin.Begin );
            header.Field14.Value = new float[20];
            for ( int i = 0; i < header.Field14.Value.Length; i++ )
                header.Field14.Value[i] = reader.ReadSingle();

            // color 3
            reader.BaseStream.Seek( OffsetBase.Peek() + header.Field18.Offset, SeekOrigin.Begin );
            header.Field18.Value = new float[20];
            for ( int i = 0; i < header.Field18.Value.Length; i++ )
                header.Field18.Value[i] = reader.ReadSingle();

            // content type 5
            reader.BaseStream.Seek( OffsetBase.Peek() + header.Cameras.Offset, SeekOrigin.Begin );
            header.Cameras.Value = new OffsetTo<Camera>[header.CameraCount];
            Trace.WriteLine( $"Reading {header.CameraCount} camera entries @ {reader.BaseStream.Position:X8}" );

            for ( int i = 0; i < header.Cameras.Value.Length; i++ )
                header.Cameras.Value[i].Offset = reader.ReadUInt32();

            Trace.Indent();
            for ( int i = 0; i < header.Cameras.Value.Length; i++ )
            {
                reader.BaseStream.Seek( OffsetBase.Peek() + header.Cameras.Value[i].Offset, SeekOrigin.Begin );
                Trace.WriteLine( $"Reading camera #{i:D2} @ {reader.BaseStream.Position:X8}" );
                Trace.Indent();
                header.Cameras.Value[i].Value = ReadCamera( reader );
                Trace.Unindent();
            }
            Trace.Unindent();

            // shape
            reader.BaseStream.Seek( OffsetBase.Peek() + header.Shape.Offset, SeekOrigin.Begin );

            Trace.WriteLine( $"Reading shape @ {reader.BaseStream.Position:X8}" );
            Trace.Indent();
            header.Shape.Value = ReadShape( reader );
            Trace.Unindent();

            // animation entries
            reader.BaseStream.Seek( OffsetBase.Peek() + header.AnimationEntries.Offset, SeekOrigin.Begin );
            header.AnimationEntries.Value = new AnimationEntry[header.AnimationCount];
            Trace.WriteLine( $"Reading {header.AnimationCount} animation entries @ {reader.BaseStream.Position:X8}" );

            Trace.Indent();
            for ( int i = 0; i < header.AnimationEntries.Value.Length; i++ )
            {
                Trace.WriteLine( $"Reading content 6 entry #{i:D2} @ {reader.BaseStream.Position:X8}" );
                Trace.Indent();
                header.AnimationEntries.Value[i] = ReadAnimationEntry( reader );
                Trace.Unindent();
            }
            Trace.Unindent();

            reader.BaseStream.Position = temp;

            return header;
        }

        private static Node ReadNode( BinaryReader reader )
        {
            Node node;
            node.ParentId = reader.ReadUInt16();
            node.Id = reader.ReadUInt16();
            node.Field04 = reader.ReadUInt16();
            node.Field06 = reader.ReadUInt16();
            node.Hash = reader.ReadUInt32();
            node.Name.Offset = reader.ReadUInt32();
            node.Field10 = reader.ReadUInt32();

            long temp = reader.BaseStream.Position;

            node.Name.Value = ReadStringAt( reader, node.Name.Offset );

            reader.BaseStream.Position = temp;

            Trace.WriteLine( $"{node.Name.Value.PadRight( 10 )} {(short)node.ParentId:D2} {( short )node.Id:D2} {( short )node.Field04:D2} {( short )node.Field06:D2} {node.Hash:X8} {node.Field10:X8}" );

            return node;
        }

        private static AnimationEntry ReadAnimationEntry( BinaryReader reader )
        {
            AnimationEntry entry;
            entry.Animation.Offset = reader.ReadUInt32();
            entry.Name.Offset = reader.ReadUInt32();
            entry.Hash = reader.ReadUInt32();

            long temp = reader.BaseStream.Position;

            entry.Name.Value = ReadStringAt( reader, entry.Name.Offset );

            reader.BaseStream.Seek( OffsetBase.Peek() + entry.Animation.Offset, SeekOrigin.Begin );
            entry.Animation.Value = ReadForestContentType7Header( reader );

            reader.BaseStream.Position = temp;

            return entry;
        }

        private static Animation ReadForestContentType7Header( BinaryReader reader )
        {
            Animation header;
            header.Field00 = reader.ReadUInt32();
            header.Field04 = reader.ReadUInt32();
            header.Field08 = reader.ReadUInt32();
            header.Field0C = reader.ReadUInt32();
            header.Field10 = reader.ReadUInt32();
            header.Field14 = reader.ReadUInt32();
            header.Field18 = reader.ReadUInt32();

            return header;
        }

        private static Camera ReadCamera( BinaryReader reader )
        {
            Camera header;

            header.Field00 = reader.ReadSingle();
            header.Field04 = reader.ReadSingle();
            header.Field08 = reader.ReadSingle();
            header.Field0C = reader.ReadSingle();
            header.Field10 = reader.ReadSingle();

            return header;
        }

        private static Texture ReadForestTexture( BinaryReader reader )
        {
            Texture header;
            header.Name.Offset = reader.ReadUInt32();
            header.Field04 = reader.ReadUInt32();
            header.Offset = reader.ReadUInt32();
            header.Field0C = reader.ReadUInt32();

            long temp = reader.BaseStream.Position;
            header.Name.Value = ReadStringAt( reader, header.Name.Offset );
            reader.BaseStream.Position = temp;

            Trace.WriteLine( $"Read texture {header.Name.Value.PadRight(40)} {header.Field04:X8} {header.Offset:X8} {header.Field0C:X8}" );

            return header;
        }

        private static ForestContentType2Header ReadForestContentType2Header( BinaryReader reader )
        {
            ForestContentType2Header header;
            header.Hash = reader.ReadUInt32();
            header.Count = reader.ReadUInt32();
            header.Hashes.Offset = reader.ReadUInt32();

            Trace.WriteLine( $"Hash: {header.Hash:X8} reading {header.Count:D2} hashes" );

            long temp = reader.BaseStream.Position;
            reader.BaseStream.Seek( OffsetBase.Peek() + header.Hashes.Offset, SeekOrigin.Begin );
            header.Hashes.Value = new uint[header.Count];
            Trace.Indent();
            for ( int i = 0; i < header.Hashes.Value.Length; i++ )
            {
                header.Hashes.Value[i] = reader.ReadUInt32();
                Trace.WriteLine( $"Hash #{i:D2} {header.Hashes.Value[i]:X8}" );
            }
            Trace.Unindent();
            reader.BaseStream.Position = temp;

            return header;
        }

        private static Shape ReadShape( BinaryReader reader )
        {
            Shape shape;
            shape.Field00 = new float[4];
            for ( int i = 0; i < shape.Field00.Length; i++ )
                shape.Field00[i] = reader.ReadSingle();

            shape.MeshCount = reader.ReadUInt32();
            shape.Meshes.Offset = reader.ReadUInt32();
            shape.Field18 = reader.ReadUInt32();
            shape.Field1C = reader.ReadUInt32();
            shape.Field20 = reader.ReadUInt32();
            shape.Name.Offset = reader.ReadUInt32();

            long temp = reader.BaseStream.Position;

            shape.Name.Value = ReadStringAt( reader, shape.Name.Offset );
            Trace.WriteLine( $"{shape.Field18:X8} {shape.Field1C:X8} {shape.Field20:X8} {shape.Name.Value}" );

            reader.BaseStream.Seek( OffsetBase.Peek() + shape.Meshes.Offset, SeekOrigin.Begin );
            Trace.WriteLine( $"Reading {shape.MeshCount} meshes @ {reader.BaseStream.Position:X8}" );
            shape.Meshes.Value = new OffsetTo<Mesh>[shape.MeshCount];
            for ( int i = 0; i < shape.Meshes.Value.Length; i++ )
                shape.Meshes.Value[i].Offset = reader.ReadUInt32();

            for ( int i = 0; i < shape.Meshes.Value.Length; i++ )
            {
                reader.BaseStream.Seek( OffsetBase.Peek() + shape.Meshes.Value[i].Offset, SeekOrigin.Begin );
                Trace.WriteLine( $"Reading mesh #{i:D2} @ {reader.BaseStream.Position:X8}" );
                Trace.Indent();
                shape.Meshes.Value[i].Value = ReadMesh( reader );
                Trace.Unindent();
            }

            reader.BaseStream.Position = temp;

            return shape;
        }

        private static Mesh ReadMesh( BinaryReader reader )
        {
            Mesh mesh;
            mesh.Field00 = new float[24];
            for ( int i = 0; i < mesh.Field00.Length; i++ )
                mesh.Field00[i] = reader.ReadSingle();

            mesh.Field60 = new uint[8];
            for ( int i = 0; i < mesh.Field60.Length; i++ )
                mesh.Field60[i] = reader.ReadUInt32();

            mesh.Field80 = reader.ReadUInt32();
            mesh.Field84 = reader.ReadUInt32();
            mesh.Field88 = reader.ReadUInt32();
            mesh.ElementCount = reader.ReadUInt32();
            mesh.ElementBufferOffset = reader.ReadUInt32();
            mesh.Field94 = reader.ReadUInt32();
            mesh.VertexBufferDescriptor.Offset = reader.ReadUInt32();
            mesh.Field9C = reader.ReadUInt32();
            mesh.Field100 = reader.ReadUInt32();
            mesh.Field104 = reader.ReadUInt32();

            Trace.WriteLine( $"{mesh.Field80:X8} {mesh.Field84:X8} {mesh.Field88:X8} {mesh.ElementCount:X8} {mesh.ElementBufferOffset:X8} {mesh.Field94:X8} {mesh.Field9C:X8} {mesh.Field100:X8} {mesh.Field104:X8}" );

            long temp = reader.BaseStream.Position;

            reader.BaseStream.Seek( OffsetBase.Peek() + mesh.VertexBufferDescriptor.Offset, SeekOrigin.Begin );
            Trace.WriteLine($"Reading mesh buffer descriptor @ {reader.BaseStream.Position:X8}");
            Trace.Indent();
            mesh.VertexBufferDescriptor.Value = ReadMeshBufferDescriptor( reader );
            Trace.Unindent();

            reader.BaseStream.Position = temp;

            return mesh;
        }

        private static MeshVertexBufferDescriptor ReadMeshBufferDescriptor( BinaryReader reader )
        {
            MeshVertexBufferDescriptor descriptor;
            descriptor.Field00 = reader.ReadUInt32();
            descriptor.Field04 = reader.ReadUInt32();
            descriptor.Field08 = reader.ReadUInt32();
            descriptor.VertexStride = reader.ReadUInt32();
            descriptor.VertexCount = reader.ReadUInt32();
            descriptor.VertexBufferOffset = reader.ReadUInt32();
            descriptor.Field18 = reader.ReadUInt32();
            descriptor.Field1C = reader.ReadUInt32();
            descriptor.Field20 = reader.ReadUInt32();
            descriptor.Field24 = reader.ReadUInt32();
            descriptor.Field28 = reader.ReadUInt32();
            descriptor.Field2C = reader.ReadUInt32();
            descriptor.Field30 = reader.ReadUInt32();
            descriptor.Field34 = reader.ReadUInt32();

            TraceAllFields( descriptor );

            return descriptor;
        }

        private static void TraceField( object instance, string name, bool hex )
        {
            Trace.WriteLine( $"{name}: {instance.GetType().GetField( name ).GetValue( instance ):X8}" );
        }

        private static void TraceAllFields( object instance )
        {
            foreach ( var field in instance.GetType().GetFields() )
            {
                Trace.WriteLine( $"{field.Name}: {field.GetValue( instance ):X8}" );
            }
        }
    }

    struct OffsetTo<T>
    {
        public uint Offset;
        public T Value;

        public OffsetTo( uint offset, T value )
        {
            Offset = offset;
            Value = value;
        }

        public override string ToString()
        {
            return $"{Value} @ {Offset:X8}";
        }
    }

    struct ForestFile
    {
        public uint FileSize;
        public List<ForestChunk> Chunks;
    }

    struct ForestChunk
    {
        public ForestChunkHeader Header;
        public object Content;
    }

    struct ForestChunkHeader
    {
        public string FourCC;
        public uint ChunkSize;
        public uint ContentSize;
        public uint Field0C;
    }

    struct ForestListHeader
    {
        public uint ForestCount;
        public ForestEntryHeader[] Entries;
    }

    struct ForestEntryHeader
    {
        public uint Field00;
        public OffsetTo<string> Name;
        public OffsetTo<ForestHeader> ForestHeader;
        public uint Field0C;
    }

    struct ForestHeader
    {
        public uint ContentType0Count;
        public OffsetTo<OffsetTo<ForestContentType0Header>[]> ContentType0Array;
        public uint TextureCount;
        public OffsetTo<OffsetTo<Texture>[]> Textures;
        public uint ContentType2Count;
        public OffsetTo<ForestContentType2Header[]> ContentType2Array;
        public uint ContentType3Count;
        public uint ContentType3ArrayOffset;
        public uint Field20;
    }

    struct ForestContentType0Header
    {
        public uint Field00;
        public uint Hash;
        public uint NodeCount;
        public OffsetTo<OffsetTo<Node>[]> Nodes;
        public OffsetTo<float[]> Field10;
        public OffsetTo<float[]> Field14;
        public OffsetTo<float[]> Field18;
        public uint Field1C;
        public uint Field20;
        public uint Field24;
        public uint Field28;
        public uint Field2C;
        public uint CameraCount;
        public OffsetTo<OffsetTo<Camera>[]> Cameras;
        public uint Field38;
        public uint Field3C;
        public uint Field40;
        public uint Field44;
        public OffsetTo<Shape> Shape;
        public uint AnimationCount;
        public OffsetTo<AnimationEntry[]> AnimationEntries;
        public uint Field54;
        public uint Field58;
        public uint Field5C;
        public uint Field60;
    }

    struct Texture
    {
        public OffsetTo<string> Name;
        public uint Field04;
        public uint Offset;
        public uint Field0C;
    }

    struct ForestContentType2Header
    {
        public uint Hash;
        public uint Count;
        public OffsetTo<uint[]> Hashes;
    }

    struct ForestContentType3Header
    {

    }

    struct Node
    {
        public ushort ParentId;
        public ushort Id;
        public ushort Field04;
        public ushort Field06;
        public uint Hash;
        public OffsetTo<string> Name;
        public uint Field10;
    }

    struct Camera
    {
        public float Field00;
        public float Field04;
        public float Field08;
        public float Field0C;
        public float Field10;
    }

    struct AnimationEntry
    {
        public OffsetTo<Animation> Animation;
        public OffsetTo<string> Name;
        public uint Hash;
    }

    struct Animation
    {
        public uint Field00;
        public uint Field04;
        public uint Field08;
        public uint Field0C;
        public uint Field10;
        public uint Field14;
        public uint Field18;
    }

    struct Shape
    {
        public float[] Field00;
        public uint MeshCount;
        public OffsetTo<OffsetTo<Mesh>[]> Meshes;
        public uint Field18;
        public uint Field1C;
        public uint Field20;
        public OffsetTo<string> Name;
    }

    struct Mesh
    {
        public float[] Field00;
        public uint[] Field60;
        public uint Field80;
        public uint Field84;
        public uint Field88;
        public uint ElementCount;
        public uint ElementBufferOffset;
        public uint Field94;
        public OffsetTo<MeshVertexBufferDescriptor> VertexBufferDescriptor;
        public uint Field9C;
        public uint Field100;
        public uint Field104;
    }

    struct MeshVertexBufferDescriptor
    {
        public uint Field00;
        public uint Field04;
        public uint Field08;
        public uint VertexStride;
        public uint VertexCount;
        public uint VertexBufferOffset;
        public uint Field18;
        public uint Field1C;
        public uint Field20;
        public uint Field24;
        public uint Field28;
        public uint Field2C;
        public uint Field30;
        public uint Field34;
    }

}
