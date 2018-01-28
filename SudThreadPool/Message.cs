using System;

namespace ConsoleApp1
{
    public class Message : BaseMessage
    {

        public string msg { get; set; }
    }

    public class BaseMessage : IComparable
    {
        public int msgId { get; set; }

        public int CompareTo(Object ob)
        {
            throw new NotImplementedException();
            //return this.msgId==(int)ob?1:0;
        }
    }
}
