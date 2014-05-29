using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.Drawing;

namespace MDLFileReaderWriter.MDLFile
{

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ZString
    {
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ZStringMarshaler))]
        public string Str;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AllegianceSymbolObject
    {
        public Int32 SymbolIndex;
        public Object Obj;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct MeshGeo
    {
        public Int32 CountVerticies;
        public Int32 CountIndicies;
        public Vertex[] Vertices;
        public UInt16[] Faces;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct GroupGeo
    {
        public List<Object> Geos;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LODGeo
    {
        public float LOD;
        public List<Object> Meshes;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct LODGeos
    {
        public object Geo;
        public List<LODGeo> LODs;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct BinarySurfaceInfo
    {
        public WinPoint Size;
        public Int32 Pitch;
        public Int32 BitCount;
        public Int32 RedMask;
        public Int32 GreenMask;
        public Int32 BlueMask;
        public Int32 AlphaMask;
        public Boolean ColorKey;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct WinPoint
    {
        public Int32 X;
        public Int32 Y;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct ModifiableNumber
    {
        public Int32 Value;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct TextureGeo
    {
        public MeshGeo Mesh;
        public SymbolPair Texture;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Vertex
    {
        public float x;
        public float y;
        public float z;
        public float u;
        public float v;
        public float nx;
        public float ny;
        public float nz;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Frame
    {
        public float Value1;
        public object Value2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Boxed
    {
        public Int32 Value1;
        public object Value2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct Header
    {
        public UInt32 magic;
        public Int32 version;
        public Int32 countNameSpaces;
        public Int32 countImports;
        public Int32 countExports;
        public Int32 countDefinitions;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct AlignedBytes
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        internal char[] chars;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct SymbolPair
    {
        public Int32 Value;
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ZStringMarshaler))]
        public string Name;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct Light
    {
        public float red;
        public float green;
        public float blue;
        public float speed; // or time factor
        public float posx;
        public float posy;
        public float posz;
        public float todo1; // 1.25 (0 = crash !)
        public float todo2; // 0
        public float todo3; // 0.1
        public float todo4; // 0
        public float todo5; // 0.05
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct LightsGeo
    {
        public Light[] Lights;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct FrameData
    {
        public int Count;
        public Data[] Datas;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct Data
    {
        [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(ZStringMarshaler))]
        public string Name;
        public float posx;
        public float posy;
        public float posz;
        public float nx;
        public float ny;
        public float nz;
        public float px;
        public float py;
        public float pz;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    internal struct FrameImage
    {
        public Bitmap Background;
        public List<Bitmap> Surfaces;
        public Int32 NumFrames;
        public Int32[] Offsets;
    }
}
