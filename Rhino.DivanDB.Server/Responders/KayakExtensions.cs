using System;
using System.Collections.Generic;
using System.IO;
using Kayak;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Linq;

namespace Rhino.DivanDB.Server.Responders
{
    public static class KayakExtensions
    {
        public static JObject ReadJson(this KayakContext context)
        {
            using (var streamReader = new StreamReader(context.Request.InputStream))
            using (var jsonReader = new JsonTextReader(streamReader))
                return JObject.Load(jsonReader);
        }

        public static string ReadString(this KayakContext context)
        {
            using (var streamReader = new StreamReader(context.Request.InputStream))
                return streamReader.ReadToEnd();
        }

        public static void WriteJson(this KayakContext context, object obj)
        {
            new JsonSerializer
            {
                Converters = {new JsonToJsonConverter()}
            }.Serialize(context.Response.Output, obj);
        }

        public static void WriteData(this KayakContext context, byte[] data)
        {
            if (data == null)
            {
                context.Response.SetStatusToNotFound();
            }
            else
            {
                Stream stream = context.Response.GetDirectOutputStream(data.Length);
                stream.Write(data, 0, data.Length);
            }
        }

        public static void SetStatusToDeleted(this KayakResponse response)
        {
            response.StatusCode = 204;
            response.ReasonPhrase = "No Content";
        }

        /// <summary>
        /// Reads the entire request buffer to memory and
        /// return it as a byte array.
        /// </summary>
        public static byte[] ReadData(this KayakContext context)
        {
            var list = new List<byte[]>();
            var inputStream = context.Request.InputStream;
            const int defaultBufferSize = 1024*16;
            var buffer = new byte[defaultBufferSize];
            int offset = 0;
            int read;
            while ((read = inputStream.Read(buffer, offset, buffer.Length - offset)) != 0)
            {
                offset += read;
                if (offset == buffer.Length)
                {
                    list.Add(buffer);
                    buffer = new byte[defaultBufferSize];
                    offset = 0;
                }
            }
            int totalSize = list.Sum(x => x.Length) + offset;
            var result = new byte[totalSize];
            var resultOffset = 0;
            foreach (var partial in list)
            {
                Buffer.BlockCopy(partial, 0, result, resultOffset, partial.Length);
                resultOffset += partial.Length;
            }
            Buffer.BlockCopy(buffer, 0, result, resultOffset, offset);
            return result;
        }

        #region Nested type: JsonToJsonConverter

        public class JsonToJsonConverter : JsonConverter
        {
            public override
                void WriteJson
                (JsonWriter writer, object value)
            {
                ((JObject) value).WriteTo(writer);
            }

            public override
                object ReadJson
                (JsonReader reader, Type objectType)
            {
                throw new NotImplementedException();
            }

            public override
                bool CanConvert
                (Type
                     objectType)
            {
                return objectType == typeof (JObject);
            }
        }

        #endregion
    }
}