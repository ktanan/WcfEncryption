using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace WcfEncryption
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService" in both code and config file together.
    [ServiceContract]
    public interface IService
    {
        [OperationContract]
        void DoWork();

        [OperationContract]
        Wcf_Response_PinEncrypt PinEncrypt(string PIPIN,string PIRLCRD);

    }

    [DataContract]
    public struct Wcf_Response_PinEncrypt
    {
        [DataMember]
        public string POMCHKEY;
        [DataMember]
        public string POPINPACK;
        [DataMember]
        public string POMSG;
    }   

}
