using Casimodo.Lib.Data;
using Ionic.Zip;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;

namespace Casimodo.Lib.Mojen
{
    public static class MojenMetaSerializer
    {
        // TODO: REMOVE: static readonly MyWriter _writer = new MyWriter();

        static readonly DataContractSerializerSettings _serializerSettings = new DataContractSerializerSettings
        {
            PreserveObjectReferences = true,
            KnownTypes = new Type[]
            {
                typeof(string[]),
                typeof(MojenMetaContainer),
                typeof(AppBuildConfig),
                typeof(DataLayerConfig),
                typeof(DateTimeOffset),
                typeof(MojType), typeof(MojProp), typeof(MojType),
                typeof(MojProp), typeof(MojAttrs),
                typeof(MojAttr), typeof(MojAttrArg),
                typeof(MojValueSetContainer), typeof(MojValueSetAggregate), typeof(MojValueSet), typeof(MojValueSet<string>),
                typeof(MojOrderConfig), typeof(MojSortDirection),
                typeof(MojPickConfig), typeof(MojVersionMapping),
                typeof(MojDateTimeInfo), typeof(MojBinaryConfig),
                typeof(MojInterface),
                typeof(MojSequenceConfig),                
                typeof(MojTypeComparison),
                typeof(MojUsingGeneratorConfig),
                typeof(MojSummaryConfig),
                typeof(MojDefaultValueCommon),
                typeof(MojReferenceBinding),
                typeof(MojMultilineStringType),
                // Mex
                typeof(MexItem), typeof(MexProp), typeof(MexOp), typeof(MexValue), typeof(MexCondition), typeof(MexExpressionNode)
            }
        };

        public static T Deserialize<T>(string filePath)
            where T : class
        {
            return (T)Deserialize(typeof(T), filePath);
        }

        public static object Deserialize(Type type, string filePath)
        {
            //var sw = new SimpleStopwatch().Start();
            object obj;
            var serializer = new DataContractSerializer(type, _serializerSettings);
            using (var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            using (var zip = new ZipInputStream(fs))
            using (var reader = XmlDictionaryReader.CreateBinaryReader(zip, XmlDictionaryReaderQuotas.Max))
            {
                zip.GetNextEntry();
                obj = serializer.ReadObject(reader);
            }
            //sw.O("# Deserialization");
            return obj;
        }

        public static void Serialize<T>(T item, string filePath)
            where T : class
        {
            Serialize(typeof(T), item, filePath);
        }

        public static void Serialize(Type type, object item, string filePath)
        {
            //var sw = new SimpleStopwatch().Start();
            var serializer = new DataContractSerializer(type, _serializerSettings);
            using (var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None))
            using (var zip = new ZipOutputStream(fs))
            // NOTE: Zipping adds ~ 100ms (in pre .NET Core).
            using (var writer = XmlDictionaryWriter.CreateBinaryWriter(zip))
            {
                zip.ParallelDeflateThreshold = -1;
                zip.PutNextEntry("data");
                serializer.WriteObject(writer, item);
            }
            //sw.O("# Serialization");
        }

        class MyWriter : MojenGeneratorBase
        {
            public void Serialize(Type type, object item, string filePath)
            {
                PerformWrite(filePath, (stream, writer) =>
                    new DataContractSerializer(type, _serializerSettings)
                        .WriteObject(stream, item));
            }
        }
    }
}