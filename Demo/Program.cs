using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PacketBaseLib;

namespace Demo
{
    class Program
    {
        static void Main(string[] args)
        {
            PacketBase packet = new PacketBase(10);
            TestPacket test = new TestPacket(17);
        }
    }
    class TestPacket : PacketBase
    {
        public TestPacket(int length):base(length)
        {
            base.AddField<UInt32>("PkgLen",(uint)length);
            base.AddField<Byte>("Version", 0);
            //base.AddField<Command>("Command", Command.CMD_GET_AUTHCODE);
            base.AddField<UInt32>("Command", 3);
            //base.AddField<Byte[]>("Data", new byte[3]{ 0x00,0x01,0x02});
        }


    }

    enum Command
    {
        CMD_GET_AUTHCODE = 101,
        CMD_LOGIN = 103,
        CMD_GET_GOOD_SERVER_LIST = 105,
        CMD_GET_SERVER_LIST = 106
    }
}
