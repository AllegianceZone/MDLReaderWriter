using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.Diagnostics;
using System.Drawing;
using System.Collections;
using System.Drawing.Imaging;
using System.Xml.Linq;

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
            Collada,
        }

        public string ToString(OutFormat format)
        {
            switch (format)
            {
                case OutFormat.TextMdl:
                    return this.ToString();
                case OutFormat.Collada:
                    return this.ToCollada();
                default:
                    return this.ToString();
            }
        }
        private int _next= 0;
        public int Next()
        {
            return _next++;
        }
        public string ToCollada()
        {
            XNamespace x = "http://www.collada.org/2005/11/COLLADASchema";
            var xd = new XDocument(new XDeclaration("1.0", "utf-8", "yes"));
            
            XElement xe = new XElement(x+"COLLADA",  new XAttribute("version","1.4.1"));
            var asset = new XElement(x + "asset",
                            new XElement(x + "revision", "1.0"),
                            new XElement(x + "authoring_tool", "Allegiance Zone MDLd"),
                            new XElement(x + "modified", DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")));
            xe.Add(asset);
            var libGeo = new XElement(x + "library_geometries");
            foreach (var geo in Objects.Where(_ => _ is MeshGeo || _ is GroupGeo || _ is LODGeo || _ is LODGeos))
            {
                if (geo is LODGeos)
                {
                    var lodGeos = (LODGeos)geo;
                    if (lodGeos.Geo is GroupGeo)
                    {
                        var gg = (GroupGeo)lodGeos.Geo;
                        foreach(var g in gg.Geos)
                        {
                            if (g is MeshGeo)
                            {
                                var _g = (MeshGeo)g;
                                libGeo.Add(ExportMeshGeoCollada(x, _g, "FOO" + Next()));
                            }
                            if (g is TextureGeo)
                            {
                                var _t = (TextureGeo)g;
                                libGeo.Add(ExportMeshGeoCollada(x, _t.Mesh, "BAR" + Next()));

                                // add the texture


                            }
                        }
                    }


                    for(int i =0; i< lodGeos.LODs.Count; i++)
                    {
                        var item = lodGeos.LODs[i];
                        for (int j = 0; j < item.Meshes.Count; j++)
                        {
                            var mesh = item.Meshes[j];
                            if (mesh is MeshGeo)
                            {
                                var mes = (MeshGeo)mesh;
                                libGeo.Add(ExportMeshGeoCollada(x, mes, "Baz" + Next()));
                            }
                            if (mesh is TextureGeo)
                            {
                                var _t = (TextureGeo)mesh;
                                libGeo.Add(ExportMeshGeoCollada(x, _t.Mesh, "Jaz" + Next()));
                                // add the texture
                            }
                        }
                    }
                }
                if (geo is MeshGeo)
                {
                    libGeo.Add(ExportMeshGeoCollada(x, (MeshGeo)geo, "FOO"+"-mesh"));
                }
            }
            xe.Add(libGeo);
            xd.Add(xe);

            var scene = new XElement(x + "scene", new XAttribute("name", "DefaultScene"),
                                new XElement(x + "node", new XAttribute("name", "FirstExport"),
                                    new XElement(x + "instance", new XAttribute("url", "#FOO0-mesh"))
                                ));
            xe.Add(scene);
            return @"<?xml version=""1.0"" encoding=""utf-8""?>"+Environment.NewLine + xd.ToString();
            //if (Objects.Any(_ => _ is LightsGeo))
            //{
            //    // need to build a mtl files for lights.
            //    var mtlLightsLib = MtlName + "lights.mtl";
            //    var lightsMtl = new StringBuilder();
            //    sb.AppendFormat("mtllib {0}" + Environment.NewLine, mtlLightsLib);
            //    //sb.AppendLine("o alleglight");
            //    foreach (var geo in Objects.Where(_ => _ is LightsGeo).Select(_=> (LightsGeo)_))
            //    {

            //        //    var lg = (LightsGeo)ob;
            //        //    sb.AppendLine("lights = LightsGeo([");
            //        for (int i = 0; i < geo.Lights.Length; i++)
            //        {
            //            var item = geo.Lights[i];
            //             // Material name statement:
            //             //       newmtl my_mtl
                        
            //            //lightsMtl.AppendFormat("newmtl light_{0}" + Environment.NewLine, i);
            //            // see http://paulbourke.net/dataformats/mtl/ for examples, search for "shiny_green"
            //             //Material color and illumination statements:
            //             //       Ka 0.0435 0.0435 0.0435
            //             //       Kd 0.1086 0.1086 0.1086
            //             //       Ks 0.0000 0.0000 0.0000
            //             //       Tf 0.9885 0.9885 0.9885
            //             //       illum 6
            //             //       d -halo 0.6600
            //             //       Ns 10.0000
            //             //       sharpness 60
            //             //       Ni 1.19713
            //            //lightsMtl.AppendFormat("Kd {0} {1} {2}" + Environment.NewLine, item.red.ToString("0.000000"), item.green.ToString("0.000000"), item.blue.ToString("0.000000"));
            //            //lightsMtl.AppendFormat("Ns 200.000" + Environment.NewLine);
            //            //lightsMtl.AppendFormat("illum 1"+Environment.NewLine);
                        
            //            sb.AppendFormat("usemtl light_{0}"+Environment.NewLine, i);
            //            sb.AppendFormat("o alleglight_{0}" + Environment.NewLine, i);
            //            sb.AppendFormat("v {0} {1} {2}" + Environment.NewLine, item.posx.ToString("0.000000"),
            //                item.posy.ToString("R"),
            //                item.posz.ToString("R"));
            //            sb.AppendFormat("p {0}" + Environment.NewLine, i+629);
            //            sb.AppendLine();
            //            //sb.AppendLine(string.Format("(Color({0}, {1}, {2}),Vector({3},{4},{5}),{6},{7},{8},{9},{10}){11}", item.red, item.green, item.blue
            //            //    , item.posx.ToString("R"), item.posy.ToString("R"), item.posz.ToString("R")
            //            //    , item.speed.ToString("R"), item.todo1.ToString("R"), item.todo2.ToString("R"), item.todo3.ToString("R"), item.todo5.ToString("R")
            //            //    , i == geo.Lights.Length - 1 ? "" : ","));
            //        }
            //        //    sb.AppendLine("]);");
            //    }

            //    File.WriteAllText(Path.Combine(MtlPath, mtlLightsLib), lightsMtl.ToString());
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




            
        }

        public string ToObjMtl()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("# Wavefront MTL created with Allegiance Zone MDLd.exe {0}" + Environment.NewLine, DateTime.Now.ToShortDateString());

            for (int j = 0; Objects != null && j < Objects.Length; j++)
            {
                var ob = Objects[j];

                if (ob is Bitmap)
                {
                    var img = ob as Bitmap;
                    sb.AppendFormat("newmtl {0}"+Environment.NewLine,MtlName);
                    sb.AppendFormat("map_Kd {0}.png" + Environment.NewLine, MtlName);
                    img.Save(Path.Combine(MtlPath, string.Format("{0}.png",MtlName)), ImageFormat.Png);
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

        private XElement ExportMeshGeoCollada(XNamespace x, MeshGeo geo, string meshName)
        {            
            var meshId = meshName + "-mesh";
            var mesh = (MeshGeo)geo;
            XElement geometry = new XElement(x+"geometry", new XAttribute("id", meshId), new XAttribute("name", meshName),
                new XElement(x + "mesh",
                    new XElement(x + "source", new XAttribute("id", meshId + "-positions"),
                        new XElement(x + "float_array", new XAttribute("id", meshId + "-positions-array"), new XAttribute("count", (mesh.Vertices.Length * 3).ToString()),
                            string.Join(" ", mesh.Vertices.Select(_ => string.Format("{0} {1} {2}", _.x, _.y, _.z)))),
                        new XElement(x + "technique_common",
                            new XElement(x + "accessor", new XAttribute("source", "#" + meshId + "-positions-array"), new XAttribute("count", mesh.Vertices.Length), new XAttribute("stride", "3"),
                                new XElement(x + "param", new XAttribute("name", "X"), new XAttribute("type", "float")),
                                new XElement(x + "param", new XAttribute("name", "Y"), new XAttribute("type", "float")),
                                new XElement(x + "param", new XAttribute("name", "Z"), new XAttribute("type", "float"))
                                ))
                            ),
                    new XElement(x + "source", new XAttribute("id", meshId + "-normals"),
                        new XElement(x + "float_array", new XAttribute("id", meshId + "-normals-array"), new XAttribute("count", (mesh.Vertices.Length * 3).ToString()),
                            string.Join(" ", mesh.Vertices.Select(_ => string.Format("{0} {1} {2}", _.nx, _.ny, _.nz)))),
                        new XElement(x + "technique_common",
                            new XElement(x + "accessor", new XAttribute("source", "#" + meshId + "-normals-array"), new XAttribute("count", mesh.Vertices.Length), new XAttribute("stride", "3"),
                                new XElement(x + "param", new XAttribute("name", "X"), new XAttribute("type", "float")),
                                new XElement(x + "param", new XAttribute("name", "Y"), new XAttribute("type", "float")),
                                new XElement(x + "param", new XAttribute("name", "Z"), new XAttribute("type", "float"))
                                ))
                            ),
                    new XElement(x + "source", new XAttribute("id", meshId + "-map"),
                        new XElement(x + "float_array", new XAttribute("id", meshId + "-map-array"), new XAttribute("count", (mesh.Vertices.Length * 2).ToString()),
                            string.Join(" ", mesh.Vertices.Select(_ => string.Format("{0} {1}", _.u, _.v)))),
                        new XElement(x + "technique_common",
                            new XElement(x + "accessor", new XAttribute("source", "#" + meshId + "-map-array"), new XAttribute("count", mesh.Vertices.Length), new XAttribute("stride", "2"),
                                new XElement(x + "param", new XAttribute("name", "S"), new XAttribute("type", "float")),
                                new XElement(x + "param", new XAttribute("name", "T"), new XAttribute("type", "float"))
                                ))
                            ),
                    new XElement(x + "verticies", new XAttribute("id", meshId + "-verticies"),
                        new XElement(x + "input", new XAttribute("semantic", "POSITION"), new XAttribute("source", meshId + "-positions-array"))),
                    new XElement(x + "polylist", new XAttribute("material", "fig11bmp_009Material"), new XAttribute("count", mesh.Faces.Length),
                        new XElement(x + "input", new XAttribute("semantic", "VERTEX"), new XAttribute("source", "#" + meshId + "-verticies"), new XAttribute("offset", "0")),
                        new XElement(x + "input", new XAttribute("semantic", "NORMAL"), new XAttribute("source", "#" + meshId + "-normals"), new XAttribute("offset", "1")),
                        new XElement(x + "input", new XAttribute("semantic", "TEXCOORD"), new XAttribute("source", "#" + meshId + "-map"), new XAttribute("offset", "2")),
                        new XElement(x + "vcount", string.Join(" ", mesh.Faces.Select(_ => 3)),
                        new XElement(x + "p", string.Join(" ", mesh.Faces))
                        )
                )));
            return geometry;            
        }

        private XElement ExportTextureGeoCollada(TextureGeo geo)
        {
            //sb.AppendFormat(string.Format("mtllib {0}.mtl" + Environment.NewLine, texGeo.Texture.Name));
            //sb.AppendFormat(string.Format("usemtl {0}" + Environment.NewLine, texGeo.Texture.Name));

            //sb.AppendFormat(getGeoTextObjFormat(texGeo.Mesh));
            return new XElement("FOOOOOOO");
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

        //    if(geo is LODGeos)
        //    {
        //        var lod = (LODGeos)geo ;
        //        if(lod.Geo is GroupGeo)
        //        {
        //            var gp = (GroupGeo)lod.Geo;
        //            var txg = (TextureGeo)gp.Geos.First(x => x is TextureGeo);
        //            sb.Append( getGeoTextObjFormat(txg) );
        //        }

        //    }
        //    if (geo is IList)
        //    {
        //        // sb.AppendFormat("(");

        //        for (int k = 0; k < ((IList)geo).Count; k++)
        //        {
        //            sb.AppendFormat(getGeoText(((IList)geo)[k]));
        //        }

        //        // sb.AppendFormat(")");
        //    }
        //    return sb.ToString();
        //}

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

        public string MtlName { get; set; }

        public string MtlPath { get; set; }
    }

}
