using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;

namespace MDLFileReaderWriter.MDLFile
{
    public class ZStringMarshaler : ICustomMarshaler
    {
        static ZStringMarshaler static_instance;

        public static Int32 SizeOfZString(Stream br)
        {
            var originalPos = br.Position;
            var len = 0;
            // find the end of the string
            // its 4 byte aligned, so we always at least 4, so use a 'do'
            while (br.ReadByte() != 0)
            {
                len += 1;
            }
            br.Seek(originalPos, SeekOrigin.Begin);

            var alignedSize = len + (4 - (len % 4));
            return alignedSize;
        }

        public IntPtr MarshalManagedToNative(object managedObj)
        {
            if (managedObj == null)
                return IntPtr.Zero;
            if (!(managedObj is string))
                throw new MarshalDirectiveException(
                       "ZString must be used on a string.");

            // 4byte aligned null terminated
            var alignedSize = ((string)managedObj).Length 
                + (4 -(((string)managedObj).Length % 4));

            byte[] strbuf = new byte[alignedSize]; 
            // strbuf is all nulls

            Encoding.Unicode.GetBytes((string)managedObj)
                .CopyTo(strbuf,0);
            
            IntPtr buffer = Marshal.AllocHGlobal(strbuf.Length);
            Marshal.Copy(strbuf, 0, buffer, strbuf.Length);
            return buffer;
        }

        public unsafe object MarshalNativeToManaged(IntPtr pNativeData)
        {
            byte* walk = (byte*)pNativeData;
            // find the end of the string
            // its 4 byte aligned, so we always at least 4, so use a 'do'
            do
            {
                walk += 4;
            } while (*(walk - 1) != 0);// Check if the previous end byte was null
            
            int length = (int)(walk - (byte*)pNativeData);

            byte[] strbuf = new byte[length];
            // skip the trailing null
            Marshal.Copy((IntPtr)pNativeData, strbuf, 0, length - 1);

            string data = Encoding.Default.GetString(strbuf);
            return data.TrimEnd('\0');
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            Marshal.FreeHGlobal(pNativeData);
        }

        public void CleanUpManagedData(object managedObj)
        {
        }

        public int GetNativeDataSize()
        {
            return -1;
        }

        public static ICustomMarshaler GetInstance(string cookie)
        {
            if (static_instance == null)
            {
                return static_instance = new ZStringMarshaler();
            }
            return static_instance;
        }

        private ZStringMarshaler()
        {
        }
    }
}
