﻿using Adjutant.Blam.Common;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Endian;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Reclaimer.Plugins.MetaViewer.Halo3
{
    public class MetaContext
    {
        public ICacheFile Cache { get; }
        public IIndexItem IndexItem { get; }
        public TransactionStream Transaction { get; }

        public MetaContext(ICacheFile cache, IIndexItem indexItem)
        {
            Cache = cache;
            IndexItem = indexItem;

            Transaction = new TransactionStream(new MemoryStream());
        }

        public MetaContext(ICacheFile cache, IIndexItem indexItem, TransactionStream transaction)
        {
            Cache = cache;
            IndexItem = indexItem;
            Transaction = transaction;
        }
    }
}
