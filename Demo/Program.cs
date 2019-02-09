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
            LoginPacket loginPkt = new LoginPacket();
            dynamic dLoginPkt = loginPkt as dynamic;

            loginPkt.ObjectTooLarge += (object obj, int objSize, int SizeLimit) =>
            {
                Console.WriteLine("Object Too Large. Object size is {0}. Limit {1}.", objSize, SizeLimit);
            };

            loginPkt.FieldResized += (int newLength) =>
            {
                Console.WriteLine("Field resized.");
            };

            dLoginPkt.Data = new byte[6] { 0xff, 0x11, 0x22, 0x33, 0x44, 0x55 };

            Console.WriteLine("LoginPkt\n{0}", loginPkt);
            Console.WriteLine(BitConverter.ToString(loginPkt.Raw));
            Console.ReadKey();
        }
    }
    class LoginPacket : PacketBase
    {
        public LoginPacket() : base(9)
        {
            base.AddField<UInt32>("PkgLen", (uint)base.Length);
            base.AddField<Byte>("Version", 0);
            base.AddField<Command>("Command", Command.Login);
            base.AddField<Byte[]>("Data", null, AutoResize: true);

            //PkgLen自动更新
            this.RawResized += (int newLength) =>
            {
                (this as dynamic).PkgLen = newLength;
            };
        }
    }

    enum Command
    {
        Login = 0,
        Logout = 1,
        Message = 2,
    }
}
