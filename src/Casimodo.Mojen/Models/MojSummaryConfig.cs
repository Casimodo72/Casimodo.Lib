﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Casimodo.Lib.Data;

namespace Casimodo.Lib.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojSummaryConfig
    {
        public List<string> Descriptions
        {
            get { return _descriptions ?? (_descriptions = new List<string>()); }
        }
        [DataMember]
        List<string> _descriptions;

        public List<string> Remarks
        {
            get { return _remarks ?? (_remarks = new List<string>()); }
        }
        [DataMember]
        List<string> _remarks;

        public void AssignFrom(MojSummaryConfig source)
        {
            if (source._descriptions != null)
                _descriptions = new List<string>(source.Descriptions);
            if (source._remarks != null)
                _remarks = new List<string>(source.Remarks);
        }

        [OnSerializing]
        void OnSerializing(StreamingContext context)
        {
            if (_descriptions != null && _descriptions.Count == 0)
                _descriptions = null;
            if (_remarks != null && _remarks.Count == 0)
                _remarks = null;
        }
    }
}