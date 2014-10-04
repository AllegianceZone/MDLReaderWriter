using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Collections;

namespace MDLFileReaderWriter.MDLFile
{
    public class MDLFile
    {
        // Layout
        public Header Head { get; set; }
        public ZString[] NameSpaces { get; set; }
        public SymbolPair[] ImportedSymbols { get; set; }
        public ZString[] ExportedSymbols { get; set; }
        public object[] Objects;

        public bool Load(Stream stream)
        {
            bool readToEnd = false;
            
            MemoryStream ms = null;
            if (stream.CanSeek)
            {
                long len = 0;
                len = stream.Length;
                var buff = new byte[len];
                
                stream.Read(buff, 0, (int)len);
                ms = new MemoryStream(buff);
            }
            else
            {
                ms = new MemoryStream();
                var buff = new byte[1024];
                int read = 0;
                do
                {
                    read = stream.Read(buff, 0, 1024);
                    ms.Write(buff, 0, read);
                } while (read > 0);
                ms.Position = 0;
            }
            
            using (var memfile = ms)
            {
                Head = ReadStruct<Header>(memfile);
                if (Head.magic != 0xDEBADF00)
                    return readToEnd;
                NameSpaces = ReadStruct<ZString>(memfile, Head.countNameSpaces);
                ImportedSymbols = ReadImportedSymbols(memfile, this.Head);
                ExportedSymbols = ReadStruct<ZString>(memfile, Head.countExports);
                ReadObjects(ref Objects, memfile, Head.countExports + Head.countDefinitions);
                if (memfile.Position == memfile.Length)
                {
                    readToEnd = true;
                }
            }
            return readToEnd;
        }

        public bool Load(FileInfo fi)
        {
            bool readToEnd = false;
            using (var discfile = File.OpenRead(fi.FullName))
            {
                readToEnd = Load(discfile);
            };
            
            return readToEnd;
        }

        public override string ToString()
        {
            var sb = new StringBuilder();

            if(NameSpaces!=null)
            foreach (var item in NameSpaces)
            {
                sb.AppendLine(string.Format("use \"{0}\";", item.Str));
            }

            for (int j = 0; Objects != null && j < Objects.Length; j++)
            {
                var ob = Objects[j];
                if (ob is LightsGeo)
                {
                    var lg = (LightsGeo)ob;
                    sb.AppendLine("lights = LightsGeo([");
                    for (int i = 0; i < lg.Lights.Length; i++)
                    {
                        var item = lg.Lights[i];
                        sb.AppendLine(string.Format("(Color({0}, {1}, {2}),Vector({3},{4},{5}),{6},{7},{8},{9},{10}){11}", item.red, item.green, item.blue
                            , item.posx.ToString("R"), item.posy.ToString("R"), item.posz.ToString("R")
                            , item.speed.ToString("R"), item.todo1.ToString("R"), item.todo2.ToString("R"), item.todo3.ToString("R"), item.todo5.ToString("R")
                            , i == lg.Lights.Length - 1 ? "" : ","));
                    }
                    sb.AppendLine("]);");
                }

                if (ob is FrameData)
                {
                    var fd = (FrameData)ob;
                    sb.AppendLine("frames = FrameData([");
                    for (int i = 0; i < fd.Datas.Length; i++)
                    {
                        var item = fd.Datas[i];
                        sb.AppendLine(string.Format("(\"{0}\",Vector({1},{2},{3}),Vector({4},{5},{6}),Vector({7},{8},{9})){10}", item.Name
                        , item.posx.ToString("R"), item.posy.ToString("R"), item.posz.ToString("R")
                        , item.px.ToString("R"), item.py.ToString("R"), item.pz.ToString("R")
                        , item.nx.ToString("R"), item.ny.ToString("R"), item.nz.ToString("R")
                        , i == fd.Datas.Length - 1 ? "" : ","));
                    }
                    sb.AppendLine("]);");
                }

                if (isGeo(ob))
                {
                    sb.AppendLine(string.Format("object = {0};", getGeoText(ob)));
                }

            }

            return sb.ToString();
        }

        public enum OutFormat
        {
            TextMdl,
            WaveFront_Obj
        }

        public string ToString(OutFormat format)
        {
            switch (format)
            {
                case OutFormat.TextMdl:
                    return this.ToString();
                case OutFormat.WaveFront_Obj:
                    return this.ToObj();
                default:
                    return this.ToString();
            }
        }
        public string ToObj()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("# Wavefront OBJ created with Allegiance Zone MDLd.exe {0}" +Environment.NewLine, DateTime.Now.ToShortDateString());
            
            
            //if (NameSpaces != null)
            //    foreach (var item in NameSpaces)
            //    {
            //        sb.AppendLine(string.Format("use \"{0}\";", item.Str));
            //    }

            for (int j = 0; Objects != null && j < Objects.Length; j++)
            {
                var ob = Objects[j];
                //if (ob is LightsGeo)
                //{
                //    var lg = (LightsGeo)ob;
                //    sb.AppendLine("lights = LightsGeo([");
                //    for (int i = 0; i < lg.Lights.Length; i++)
                //    {
                //        var item = lg.Lights[i];
                //        sb.AppendLine(string.Format("(Color({0}, {1}, {2}),Vector({3},{4},{5}),{6},{7},{8},{9},{10}){11}", item.red, item.green, item.blue
                //            , item.posx.ToString("R"), item.posy.ToString("R"), item.posz.ToString("R")
                //            , item.speed.ToString("R"), item.todo1.ToString("R"), item.todo2.ToString("R"), item.todo3.ToString("R"), item.todo5.ToString("R")
                //            , i == lg.Lights.Length - 1 ? "" : ","));
                //    }
                //    sb.AppendLine("]);");
                //}

                //if (ob is FrameData)
                //{
                //    var fd = (FrameData)ob;
                //    sb.AppendLine("frames = FrameData([");
                //    for (int i = 0; i < fd.Datas.Length; i++)
                //    {
                //        var item = fd.Datas[i];
                //        sb.AppendLine(string.Format("(\"{0}\",Vector({1},{2},{3}),Vector({4},{5},{6}),Vector({7},{8},{9})){10}", item.Name
                //        , item.posx.ToString("R"), item.posy.ToString("R"), item.posz.ToString("R")
                //        , item.px.ToString("R"), item.py.ToString("R"), item.pz.ToString("R")
                //        , item.nx.ToString("R"), item.ny.ToString("R"), item.nz.ToString("R")
                //        , i == fd.Datas.Length - 1 ? "" : ","));
                //    }
                //    sb.AppendLine("]);");
                //}

                if (isGeo(ob))
                {

                    sb.AppendLine(getGeoTextObjFormat(ob));
                }

            }

            return sb.ToString();
        }

        private bool isGeo(object ob)
        {
            return ob is LODGeos
                || ob is MeshGeo
                || ob is TextureGeo
                || ob is GroupGeo
                || ob is IList;
        }

        private string getGeoText(object geo)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine();
            if (geo is MeshGeo)
            {
                
                var mesh = (MeshGeo)geo;
                sb.AppendFormat("MeshGeo([");
                for (int p = 0; p < mesh.Vertices.Length; p++)
                {
                    var item = mesh.Vertices[p];
                    sb.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7}{8}", item.x.ToString("R"), item.y.ToString("R"), item.z.ToString("R"), item.nx.ToString("R"), item.ny.ToString("R"), item.nz.ToString("R"), item.u.ToString("R"), item.v.ToString("R")
                        , p == mesh.Vertices.Length - 1 ? "" : ",");
                }
                sb.AppendFormat("],");

                sb.AppendFormat("[");
                for (int p = 0; p < mesh.Faces.Length; p++)
                {
                    var item = mesh.Faces[p];
                    sb.AppendFormat(string.Format("{0}{1}", item
                        , p == mesh.Faces.Length - 1 ? "" : ","));
                }
                sb.AppendFormat("])");

            }

            if (geo is TextureGeo)
            {
                var texGeo = (TextureGeo)geo;
                sb.AppendFormat("TextureGeo(");
                sb.AppendFormat(getGeoText(texGeo.Mesh));
                sb.AppendFormat(",");
                sb.AppendFormat(string.Format("ImportImage(\"{0}\",true)", texGeo.Texture.Name));
                sb.AppendFormat(")");
            }

            if (geo is GroupGeo)
            {
                var gg = (GroupGeo)geo;
                sb.AppendFormat("GroupGeo([");
                for (int k = 0; k < gg.Geos.Count; k++)
                {
                    sb.AppendFormat(getGeoText(gg.Geos[k]));
                    sb.Append(k == gg.Geos.Count - 1 ? "" : ",");
                }
                sb.AppendFormat("])");
            }

            if (geo is LODGeos)
            {
                var lo = (LODGeos)geo;
                sb.AppendFormat("LODGeo(");

                sb.AppendFormat(getGeoText(lo.Geo));
                sb.AppendFormat(",[");
                for (int i = 0; i < lo.LODs.Count; i++)
                {
                    var item = lo.LODs[i];
                    string geoText = getGeoText(item.Meshes);
                    sb.AppendFormat(string.Format(
                        "({0},{1}){2}"
                        , item.LOD.ToString("R")
                        , geoText
                        , i == lo.LODs.Count - 1 ? "" : ","
                        ));
                }
                sb.AppendFormat("])");
            }

            if (geo is IList)
            {
               // sb.AppendFormat("(");

                for (int k = 0; k < ((IList)geo).Count; k++)
                {
                    sb.AppendFormat( getGeoText(((IList)geo)[k]));
                }

               // sb.AppendFormat(")");
            }
            sb.AppendLine();
            return sb.ToString();
        }

        private string getGeoTextObjFormat(object geo)
        {
            StringBuilder sb = new StringBuilder();
            
            if (geo is MeshGeo)
            {

                var mesh = (MeshGeo)geo;
                // Verticies
                for (int p = 0; p < mesh.Vertices.Length; p++)
                {
                    var item = mesh.Vertices[p];
                    sb.AppendFormat("v {0} {1} {2}" + Environment.NewLine, item.x.ToString("0.000000"), item.y.ToString("0.000000"), item.z.ToString("0.000000"), item.nx.ToString("R"), item.ny.ToString("R"), item.nz.ToString("R"), item.u.ToString("R"), item.v.ToString("R"), p == mesh.Vertices.Length - 1 ? "" : ",");
                }
                // Texture Coordinates
                for (int p = 0; p < mesh.Vertices.Length; p++)
                {
                    var item = mesh.Vertices[p];
                    sb.AppendFormat("vt {0} {1}" + Environment.NewLine, (1f-item.u).ToString("0.000000"), (1f-item.v).ToString("0.000000"));
                }

                // Normals
                for (int p = 0; p < mesh.Vertices.Length; p++)
                {
                    var item = mesh.Vertices[p];
                    sb.AppendFormat("vn {0} {1}" + Environment.NewLine, item.nx.ToString("0.000000"), item.ny.ToString("0.000000"), item.nz.ToString("0.000000"));
                }

                // Face Definitions
                for (int p = 0; p < mesh.Faces.Length; p+=3)
                {
                    sb.AppendFormat("f {0}/{0}/{0} {1}/{1}/{1} {2}/{2}/{2}" + Environment.NewLine, mesh.Faces[p] + 1, mesh.Faces[p + 1] + 1, mesh.Faces[p + 2] + 1);
                }
                

            }

            if (geo is TextureGeo)
            {
                var texGeo = (TextureGeo)geo;


                sb.AppendFormat(string.Format("mtllib {0}.mtl" + Environment.NewLine, texGeo.Texture.Name));
                sb.AppendFormat(string.Format("usemtl {0}" + Environment.NewLine, texGeo.Texture.Name));

                sb.AppendFormat(getGeoTextObjFormat(texGeo.Mesh));
            
                
               
            }

            //if (geo is GroupGeo)
            //{
            //    var gg = (GroupGeo)geo;
            //    sb.AppendFormat("GroupGeo([");
            //    for (int k = 0; k < gg.Geos.Count; k++)
            //    {
            //        sb.AppendFormat(getGeoText(gg.Geos[k]));
            //        sb.Append(k == gg.Geos.Count - 1 ? "" : ",");
            //    }
            //    sb.AppendFormat("])");
            //}

            //if (geo is LODGeos)
            //{
            //    var lo = (LODGeos)geo;
            //    sb.AppendFormat("LODGeo(");

            //    sb.AppendFormat(getGeoText(lo.Geo));
            //    sb.AppendFormat(",[");
            //    for (int i = 0; i < lo.LODs.Count; i++)
            //    {
            //        var item = lo.LODs[i];
            //        string geoText = getGeoText(item.Meshes);
            //        sb.AppendFormat(string.Format(
            //            "({0},{1}){2}"
            //            , item.LOD.ToString("R")
            //            , geoText
            //            , i == lo.LODs.Count - 1 ? "" : ","
            //            ));
            //    }
            //    sb.AppendFormat("])");
            //}

            if (geo is IList)
            {
                // sb.AppendFormat("(");

                for (int k = 0; k < ((IList)geo).Count; k++)
                {
                    sb.AppendFormat(getGeoText(((IList)geo)[k]));
                }

                // sb.AppendFormat(")");
            }
            return sb.ToString();
        }

        private void ReadObjects(ref object[] objs, Stream file, int ExportsAndDefCount)
        {
            objs = new object[ExportsAndDefCount];
            for (int i = 0; i < ExportsAndDefCount; i++)
            {
                var expSymbolIndex = ReadStruct<Int32>(file);
                objs[i] = ReadObject(file, expSymbolIndex);
            }
        }

        enum ObjectType
        {
            ObjectEnd = 0,
            ObjectFloat = 1,
            ObjectString = 2,
            ObjectTrue = 3,
            ObjectFalse = 4,
            ObjectList = 5,
            ObjectApply = 6,
            ObjectBinary = 7,
            ObjectReference = 8,
            ObjectImport = 9,
            ObjectPair = 10,
        }

        private object ReadObject(Stream file, Int32 expSymbolIndex)
        {
            Stack<object> stack = new Stack<object>();
            while (true)
            {
                var tokenId = (ObjectType)Enum.Parse(typeof(ObjectType), ReadStruct<Int32>(file).ToString());
                switch (tokenId)
                {
                    case ObjectType.ObjectEnd:
                        Debug.Assert(stack.Count == 1);
                        return stack.Pop();
                        break;
                    case ObjectType.ObjectFloat:
                        stack.Push(ReadStruct<float>(file));
                        break;
                    case ObjectType.ObjectString:
                        stack.Push(ReadStruct<ZString>(file));
                        break;
                    case ObjectType.ObjectTrue:
                        stack.Push(true);
                        break;
                    case ObjectType.ObjectFalse:
                        stack.Push(false);
                        break;
                    case ObjectType.ObjectList:
                        stack.Push(ReadList(file, expSymbolIndex));
                        break;
                    case ObjectType.ObjectApply:
                        stack.Push(ReadApply(stack));
                        break;
                    case ObjectType.ObjectBinary:
                        stack.Push(ReadBinary(stack, file));
                        break;
                    case ObjectType.ObjectReference:
                        stack.Push(this.Objects[ReadStruct<Int32>(file)]);
                        break;
                    case ObjectType.ObjectImport:
                        stack.Push(this.ImportedSymbols[ReadStruct<Int32>(file)]);
                        break;
                    case ObjectType.ObjectPair:
                        stack.Push(ReadPair(stack, file));
                        break;
                    default:
                        throw new Exception("ReadObject: BadToken");
                        break;
                }
            }
            return stack;
        }

        private object ReadPair(Stack<object> stack, Stream file)
        {
            var pair = new Tuple<object, object>(stack.Pop(), stack.Pop());
            return pair;
        }

        private object ReadBinary(Stack<object> stack, Stream fs)
        {
            // we should be able to pop a function pointer off the stack
            // that we could use to read from the filestream the next object
            // but.. :(
            var pop = (SymbolPair)stack.Pop();
            return ReadWithFnPtr(pop, fs, stack);
        }

        private object ReadWithFnPtr(SymbolPair fnPtr, Stream fs, Stack<object> stack)
        {
            object result = null;

            if (fnPtr.Name == "LightsGeo")
            {
                if (fnPtr.Value == 1)
                {
                    LightsGeo lp;
                    lp.Lights = ReadStruct<Light>(fs, ReadStruct<Int32>(fs));
                    return lp;
                }
            }

            if (fnPtr.Name == "FrameData")
            {
                if (fnPtr.Value == 1)
                {
                    return ReadFrameData(fs);
                }
            }

            if (fnPtr.Name == "MeshGeo")
            {
                if (fnPtr.Value == 0)
                {
                    return ReadMeshGeo(fs, 0);
                }
            }

            if (fnPtr.Name == "ImportImage")
            {
                if (fnPtr.Value == 0)
                {
                    return ReadImportImage(fs);
                }
            }

            if (fnPtr.Name == "FrameImage")
            {
                if (fnPtr.Value == 0)
                {
                    return ReadFrameImage(fs, stack);
                }
            }

            return result;
        }

        private object ReadFrameImage(Stream fs, Stack<object> stack)
        {
            var pframe = (float)stack.Pop();
            var pimage = (System.Drawing.Bitmap)stack.Pop();

            var nFrame = ReadStruct<Int32>(fs);
            var pdwOffsets = ReadStruct<Int32>(fs, nFrame); // offsets

            FrameImage fi;
            fi.Background = pimage;
            fi.NumFrames = nFrame;
            fi.Offsets = pdwOffsets;
            fi.Surfaces = new List<Bitmap>();

            for (int i = 0; i+1 < nFrame; i++)
            {
                BinarySurfaceInfo bsi;
                bsi.Size.X = pimage.Size.Width;
                bsi.Size.Y = pimage.Size.Height;
                bsi.Pitch = pimage.PixelFormat == System.Drawing.Imaging.PixelFormat.Format16bppRgb565
                    ? 16 : 24;
                bsi.BitCount = 2;
                // these arn't used so just init them.
                bsi.AlphaMask = 0;
                bsi.BlueMask = 0;
                bsi.ColorKey = false;
                bsi.GreenMask = 0;
                bsi.RedMask = 0;
                var newBitmap = i == 0 ? (Bitmap)pimage.Clone() : (Bitmap)fi.Surfaces[i - 1].Clone();
                var newBitMap = (Bitmap)ReadImportImage(fs, newBitmap,fs.Position + fi.Offsets[i], fi.Offsets[i+1]);
                fi.Surfaces.Add(newBitMap);
                if (fs.Position == fs.Length)
                {
                    // we have reached the end, and the true 
                    // nFrames
                    fi.NumFrames = i + 1;
                    break;
                }
            }
            return fi;
        }

        private Bitmap ReadImportImage(Stream fs, Bitmap toUpdate, long prle, int pend)
        {
            const ushort RLEMask       = 0xc000;
            const ushort RLEMaskFill = 0x0000;
            const ushort RLEMaskBYTE = 0x4000;
            const ushort RLEMaskWORD = 0x8000;
            const ushort RLEMaskDWORD = 0xc000;
            const ushort RLELengthMask = 0x3fff;
            Bitmap bmp = toUpdate;

            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);


            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;
            var startpos = fs.Position;
            while (prle - startpos < pend)
            {
                ushort word = ReadStruct<ushort>(fs);
                int len = word & RLELengthMask;
                prle += 2;
                var swOn = word & RLEMask;
                switch (swOn)
                {
                    case RLEMaskFill:
                        unsafe
                        {
                            for (int idx = len; idx > 0; idx--)
                            {
                                byte b = ReadStruct<byte>(fs);
                                (*(byte*)ptr) = (byte)( (*(byte*)ptr) ^ b );
                                ptr += 1;
                                prle += 1;
                            }
                        }
                        break;
                    case RLEMaskBYTE:
                        unsafe
                        {
                            byte b = ReadStruct<byte>(fs);
                            prle += 1;

                            for (int index = len; index > 0; index --) {
                                (*(byte*)ptr) = (byte)((*(byte*)ptr) ^ b);
                                ptr += 1;
                            }
                        }
                        break;
                    case RLEMaskWORD:
                        unsafe
                        {
                            UInt16 w = ReadStruct<UInt16>(fs);
                            prle += 2;

                            for (int index = len; index > 0; index--)
                            {
                                (*(UInt16*)ptr) = (UInt16) ((*(UInt16*)ptr) ^ w);
                                ptr += 2;
                            }
                        }
                        break;
                    case RLEMaskDWORD:
                            UInt32 dword = ReadStruct<UInt32>(fs);
                            prle += 4;
                            unsafe
                            {
                                for (int idx = len; idx > 0; idx--)
                                {
                                    (*(UInt32*)ptr) = (*(UInt32*)ptr) ^ dword;
                                    ptr += 4;
                                }
                            }
                        break;
                    default:
                        break;
                }
            }


            //var PixelData = ReadStruct<Byte>(fs, bmpData.Stride * toUpdate.Size.Height);




            //// Declare an array to hold the bytes of the bitmap.            
            //int numBytes = bmpData.Stride * bmpData.Height;
            //byte[] rgbValues = PixelData;//new byte[bytes];
            //bool succeded = false;
            //try
            //{
            //    // Copy the RGB values back to the bitmap
            //    System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, numBytes);
            //    succeded = true;
            //}
            //catch (Exception ex)
            //{

            //}
            //if (succeded == false)
            //{
            //    // attempt to load with accuall scanwidth in header..
            //    //numBytes = bsi.Pitch * bsi.Size.Y * (bsi.BitCount / 16);
            //    System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, numBytes);
            //}


            // Unlock the bits.
            bmp.UnlockBits(bmpData);


            return bmp;
        }

        private object ReadImportImage(Stream fs)
        {
            BinarySurfaceInfo bsi = ReadStruct<BinarySurfaceInfo>(fs);
            var PixelData = ReadStruct<Byte>(fs, bsi.Pitch * bsi.Size.Y);

            Bitmap bmp = new Bitmap(bsi.Size.X, bsi.Size.Y
                , bsi.BitCount == 16
                    ? System.Drawing.Imaging.PixelFormat.Format16bppRgb565
                    : System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;


            // Declare an array to hold the bytes of the bitmap.            
            int numBytes = bmpData.Stride * bmpData.Height;
            byte[] rgbValues = PixelData;//new byte[bytes];
            bool succeded = false;
            try
            {
                // Copy the RGB values back to the bitmap
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, numBytes);
                succeded = true;
            }
            catch (Exception ex)
            {

            }
            if (succeded == false)
            {
                // attempt to load with accuall scanwidth in header..
                numBytes = bsi.Pitch * bsi.Size.Y * (bsi.BitCount / 8);
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, numBytes);
            }


            // Unlock the bits.
            bmp.UnlockBits(bmpData);


            return bmp;
        }

        private object ReadImportImage(Stream fs, Bitmap toUpdate)
        {
            Bitmap bmp = toUpdate;

            // Lock the bitmap's bits.  
            Rectangle rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            System.Drawing.Imaging.BitmapData bmpData =
                bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite,
                bmp.PixelFormat);


            var PixelData = ReadStruct<Byte>(fs, bmpData.Stride * toUpdate.Size.Height);

            // Get the address of the first line.
            IntPtr ptr = bmpData.Scan0;


            // Declare an array to hold the bytes of the bitmap.            
            int numBytes = bmpData.Stride * bmpData.Height;
            byte[] rgbValues = PixelData;//new byte[bytes];
            bool succeded = false;
            try
            {
                // Copy the RGB values back to the bitmap
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, numBytes);
                succeded = true;
            }
            catch (Exception ex)
            {

            }
            if (succeded == false)
            {
                // attempt to load with accuall scanwidth in header..
                //numBytes = bsi.Pitch * bsi.Size.Y * (bsi.BitCount / 16);
                System.Runtime.InteropServices.Marshal.Copy(rgbValues, 0, ptr, numBytes);
            }


            // Unlock the bits.
            bmp.UnlockBits(bmpData);


            return bmp;
        }

        private object ReadApply(Stack<object> stack)
        {
            // here we should have a function pointer on the stack
            // pop it off and cast as function
            var fn = stack.Pop();
            // then we would apply that func to the rest of the stack
            // return fun->Apply(stack);
            // but we dont have the function, so we will skip that step
            // and just return a func obj with the stack.
            return new ApplyFunctionPtr(fn).Apply(stack);
        }

        public class ApplyFunctionPtr
        {
            object fnPtr;
            object ToApplyTo;
            public ApplyFunctionPtr(object fnPtr)
            {
                this.fnPtr = fnPtr;
            }
            public object Apply(Stack<object> obj)
            {
                ToApplyTo = obj;
                var sym = (SymbolPair)fnPtr;
                if (sym.Name == "ModifiableNumber")
                {
                    if (sym.Value == 0)
                    {
                        // pop a float of the stack, and return it
                        var res = Convert.ToSingle(obj.Pop());
                        return res;
                    }
                }

                if (sym.Name == "TextureGeo")
                {
                    if (sym.Value == 0)
                    {
                        // return ReadMeshGeo(fs, 0);
                        // pop a geo off the stack
                        // pop an image off the stack
                        TextureGeo tg;
                        tg.Mesh = (MeshGeo)obj.Pop();
                        tg.Texture = (SymbolPair)obj.Pop();
                        return tg;
                    }
                }

                if (sym.Name == "GroupGeo")
                {
                    if (sym.Value == 0)
                    {
                        // create our GroupGeoList
                        var groupGeo = new List<object>();

                        // pop a list of the stack
                        var list = obj.Pop() as List<object>;

                        // cast each object in the list to a Geo, and add to group geo
                        foreach (var item in list)
                        {
                            // should cast them here.
                            groupGeo.Add(item);
                        }
                        GroupGeo gg;
                        gg.Geos = groupGeo;

                        return gg;
                    }
                }

                if (sym.Name == "LODGeo")
                {
                    if (sym.Value == 0)
                    {
                        // pop a geo
                        var geo = obj.Pop();
                        // pop a list lod
                        var listLOD = obj.Pop() as List<object>;
                        // create a lodGEO
                        var LODGEO = new List<LODGeo>();

                        foreach (Tuple<object, object> item in listLOD)
                        {
                            LODGeo lodgeo;
                            lodgeo.LOD = (float)item.Item1;
                            if(item.Item2 is List<object>)
                                lodgeo.Meshes = item.Item2 as List<object>;
                            else
                                lodgeo.Meshes = new List<object>(new object[] {item.Item2});
                            LODGEO.Add(lodgeo);
                        }
                        LODGeos ja;
                        ja.Geo = geo;
                        ja.LODs = LODGEO;
                        return ja;
                    }
                }

                return this;
            }
        }

        private List<object> ReadList(Stream file, Int32 expSymbolIndex)
        {
            var list = new List<object>();
            var count = ReadStruct<Int32>(file);
            for (int i = 0; i < count; i++)
            {
                var obj = ReadObject(file, expSymbolIndex);
                list.Add(obj);
            }
            return list;
        }

        private SymbolPair[] ReadImportedSymbols(Stream file, Header header)
        {
            var ns = ReadStruct<SymbolPair>(file, header.countImports);
            return ns;
        }

        public T ReadStruct<T>(Stream fs) where T : struct
        {
            // handle ZString
            Int32 size = 0;
            try
            {
                size = Marshal.SizeOf(typeof(T));
            }
            catch (System.ArgumentException e)
            {
                return ReadStructNoSize<T>(fs);
            }

            byte[] buffer = new byte[size];

            fs.Read(buffer, 0, size);

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            T temp = (T)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));

            handle.Free();

            return temp;
        }

        public object ReadStruct(Stream fs, Type T)
        {
            // handle ZString
            Int32 size = 0;

            if (T == typeof(string))
            {
                size = ZStringMarshaler.SizeOfZString(fs);
            }
            else
            {
                size = Marshal.SizeOf(T);
            }

            byte[] buffer = new byte[size];

            fs.Read(buffer, 0, size);

            GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            object temp = null;
            if (T == typeof(string))
            {
                temp = ZStringMarshaler.GetInstance("").MarshalNativeToManaged(handle.AddrOfPinnedObject());
            }
            else
            {
                temp = Marshal.PtrToStructure(handle.AddrOfPinnedObject(), T);
            }

            handle.Free();

            return temp;
        }

        public T ReadStructNoSize<T>(Stream fs) where T : struct
        {
            T obj = new T();
            var boxedObj = (object)obj;
            var fields = typeof(T).GetFields();
            foreach (var item in fields)
            {
                object value = ReadStruct(fs, item.FieldType);

                item.SetValue(boxedObj, value);

            }

            return (T)boxedObj;
        }

        public T[] ReadStruct<T>(Stream fs, int count) where T : struct
        {
            T[] temp = new T[count];
            for (int i = 0; i < count; i++)
            {
                temp[i] = ReadStruct<T>(fs);
            }
            return temp;
        }

        public MeshGeo ReadMeshGeo(Stream fs, Int32 expSymbolIndex)
        {
            MeshGeo mesh;
            mesh.CountVerticies = ReadStruct<Int32>(fs);
            mesh.CountIndicies = ReadStruct<Int32>(fs);
            mesh.Vertices = ReadStruct<Vertex>(fs, mesh.CountVerticies);
            mesh.Faces = ReadStruct<UInt16>(fs, mesh.CountIndicies);
            return mesh;
        }

        internal FrameData ReadFrameData(Stream fs)
        {
            FrameData fdata;
            fdata.Count = ReadStruct<Int32>(fs);
            fdata.Datas = ReadStruct<Data>(fs, fdata.Count);
            return fdata;
        }
    }

}
