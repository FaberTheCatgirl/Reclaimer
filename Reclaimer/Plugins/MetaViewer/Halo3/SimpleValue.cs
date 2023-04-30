﻿using Newtonsoft.Json.Linq;
using Reclaimer.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Reclaimer.Plugins.MetaViewer.Halo3
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

        public SimpleValue(XmlNode node, MetaContext context, EndianReader reader, long baseAddress)
            : base(node, context, reader, baseAddress)
        {
            ReadValue(reader);
        }

        public override void ReadValue(EndianReader reader)
        {
            IsBusy = true;
            IsEnabled = true;

            try
            {
                reader.Seek(ValueAddress, SeekOrigin.Begin);

                switch (FieldDefinition.ValueType)
                {
                    case MetaValueType.SByte: Value = reader.ReadSByte(); break;
                    case MetaValueType.Int16: Value = reader.ReadInt16(); break;
                    case MetaValueType.Int32: Value = reader.ReadInt32(); break;
                    case MetaValueType.Int64: Value = reader.ReadInt64(); break;
                    case MetaValueType.Byte: Value = reader.ReadByte(); break;
                    case MetaValueType.UInt16: Value = reader.ReadUInt16(); break;
                    case MetaValueType.UInt32: Value = reader.ReadUInt32(); break;
                    case MetaValueType.UInt64: Value = reader.ReadUInt64(); break;

                    case MetaValueType.Angle:
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

            IsBusy = false;
        }

        public override void WriteValue(EndianWriter writer)
        {
            writer.Seek(ValueAddress, SeekOrigin.Begin);

            var parsed = float.Parse(Value.ToString());

            switch (FieldDefinition.ValueType)
            {
                case MetaValueType.SByte: writer.Write((sbyte)parsed); break;
                case MetaValueType.Int16: writer.Write((short)parsed); break;
                case MetaValueType.Int32: writer.Write((int)parsed); break;
                case MetaValueType.Int64: writer.Write((long)parsed); break;
                case MetaValueType.Byte: writer.Write((byte)parsed); break;
                case MetaValueType.UInt16: writer.Write((ushort)parsed); break;
                case MetaValueType.UInt32: writer.Write((uint)parsed); break;
                case MetaValueType.UInt64: writer.Write((ulong)parsed); break;

                case MetaValueType.Angle:
                case MetaValueType.Float32:
                    writer.Write((float)parsed);
                    break;

                case MetaValueType.Undefined:
                default:
                    writer.Write((int)parsed);
                    break;
            }

            IsDirty = false;
        }

        public override JToken GetJValue() => new JValue(Value);

        public void SetValue(object value)
        {
            _value = value;
            RaisePropertyChanged(nameof(Value));
        }
    }
}
