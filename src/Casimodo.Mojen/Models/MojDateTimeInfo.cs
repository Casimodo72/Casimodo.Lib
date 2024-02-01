using Casimodo.Lib.Data;
using System.Runtime.Serialization;

namespace Casimodo.Mojen
{
    [DataContract(Namespace = MojContract.Ns)]
    public class MojDateTimeInfo : MojBase
    {
        public MojDateTimeInfo()
        { }

        [DataMember]
        public bool IsDate { get; set; }

        [DataMember]
        public bool IsDateOnly { get; set; }

        [DataMember]
        public bool IsTime { get; set; }

        public bool IsDateAndTime
        {
            get { return IsDate && IsTime; }
        }

        [DataMember]
        public bool IsLocal { get; set; }

        /// <summary>
        /// The number of digits of milliseconds to *display*.
        /// NOTE: This does not set the number of digits of milliseconds saved to DB.
        /// </summary>
        [DataMember]
        public int DisplayMillisecondDigits { get; set; }

        public MojDateTimeInfo Clone()
        {
            return (MojDateTimeInfo)MemberwiseClone();
        }
    }

    [DataContract(Namespace = MojContract.Ns)]
    public class MojTimeSpanInfo : MojBase
    {
        public MojTimeSpanInfo()
        { }

        [DataMember]
        public bool IsDays { get; set; }

        [DataMember]
        public bool IsHours { get; set; }

        [DataMember]
        public bool IsMinutes { get; set; }

        public MojDateTimeInfo Clone()
        {
            return (MojDateTimeInfo)MemberwiseClone();
        }
    }
}