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
            string[] str = test.GetDynamicMemberNames().ToArray();
            for (int i = 0; i < str.Length; i++)
            {
                Console.WriteLine("{0}: {1}", str[i], test.Get(str[i]));
            }
            Console.ReadKey();
        }
    }
    class TestPacket : PacketBase
    {
        public TestPacket(int length):base(length)
        {
            base.AddField<UInt32>("PkgLen",(uint)length);
            base.AddField<Byte>("Version", 0);
            //base.AddField<Command>("Command", Command.Login);
            base.AddField<UInt32>("Command", 3);
            //base.AddField<Byte[]>("Data", new byte[3]{ 0x00,0x01,0x02});
        }


    }

    enum Command
    {
        Login = 0,
        Logout = 1,
        Message = 2
    }
}
