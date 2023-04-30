﻿using Newtonsoft.Json.Linq;
using Reclaimer.Blam.Halo5;
using Reclaimer.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Reclaimer.Plugins.MetaViewer.Halo5
{
    public class SimpleValue : MetaValue
    {
        public override string EntryString => Value.ToString();

        private object _value;
        public object Value
        {
            get => _value;
            set => SetMetaProperty(ref _value, value);
        }

        public SimpleValue(XmlNode node, ModuleItem item, MetadataHeader header, DataBlock host, EndianReader reader, long baseAddress, int offset)
            : base(node, item, header, host, reader, baseAddress, offset)
        {
            ReadValue(reader);
        }

        public override void ReadValue(EndianReader reader)
        {
            IsEnabled = true;

            try
            {
                reader.Seek(ValueAddress, SeekOrigin.Begin);

                switch (FieldDefinition.ValueType)
                {
                    case MetaValueType.Byte:
                        Value = reader.ReadByte();
                        break;
                    case MetaValueType.Int16:
                        Value = reader.ReadInt16();
                        break;
                    case MetaValueType.Int32:
                        Value = reader.ReadInt32();
                        break;
                    case MetaValueType.Int64:
                        Value = reader.ReadInt64();
                        break;
                    case MetaValueType.Float32:
                        Value = reader.ReadSingle();
                        break;

                    case MetaValueType.Undefined:
                    default:
                        Value = reader.ReadInt32();
                        break;
                }

                IsDirty = false;
            }
            catch { IsEnabled = false; }
        }

        public override void WriteValue(EndianWriter writer)
        {
            writer.Seek(ValueAddress, SeekOrigin.Begin);

            switch (FieldDefinition.ValueType)
            {
                case MetaValueType.Byte:
                    writer.Write((byte)Value);
                    break;
                case MetaValueType.Int16:
                    writer.Write((short)Value);
                    break;
                case MetaValueType.Int32:
                    writer.Write((int)Value);
                    break;
                case MetaValueType.Int64:
                    writer.Write((long)Value);
                    break;
                case MetaValueType.Float32:
                    writer.Write((float)Value);
                    break;

                case MetaValueType.Undefined:
                default:
                    writer.Write((int)Value);
                    break;
            }

            IsDirty = false;
        }

        public override JToken GetJValue() => new JValue(Value);
    }
}
