﻿using Hazel;
using PeasAPI.Options;
using Reactor.Networking.Attributes;
using Reactor.Networking.Rpc;

namespace PeasAPI.CustomRpc
{
    [RegisterCustomRpc((uint) CustomRpcCalls.UpdateSetting)]
    public class RpcUpdateSetting : PlayerCustomRpc<PeasAPI, RpcUpdateSetting.Data>
    {
        public RpcUpdateSetting(PeasAPI plugin, uint id) : base(plugin, id)
        {
        }

        public readonly struct Data
        {
            public readonly CustomOption Option;
            public readonly object Value;
            public readonly object Value2;

            public Data(CustomOption option, object value, object value2 = null)
            {
                Option = option;
                Value = value;
                Value2 = value2;
            }
        }

        public override RpcLocalHandling LocalHandling => RpcLocalHandling.None;

        public override void Write(MessageWriter writer, Data data)
        {
            writer.Write(data.Option.Id);
            
            if (data.Option.GetType() == typeof(CustomToggleOption))
                writer.Write((bool) data.Value);
            else if (data.Option.GetType() == typeof(CustomNumberOption))
                writer.Write((float) data.Value);
            else if (data.Option.GetType() == typeof(CustomStringOption))
                writer.Write((int) data.Value);
            else if (data.Option.GetType() == typeof(CustomRoleOption))
            {
                writer.Write((int)data.Value);
                writer.Write((int)data.Value2);
            }

            //PeasApi.Logger.LogInfo("1: " + data.Option.Id + " " + data.Value);
        }

        public override Data Read(MessageReader reader)
        {
            //PeasApi.Logger.LogInfo("2");
            var id = reader.ReadString();
            //PeasApi.Logger.LogInfo("2b: " + id);
            var option = OptionManager.CustomOptions.Find(_option => _option.Id == id);
            object value = null;
            object value2 = null;

            if (option.GetType() == typeof(CustomToggleOption))
                value = reader.ReadBoolean();
            else if (option.GetType() == typeof(CustomNumberOption))
                value = reader.ReadSingle();
            else if (option.GetType() == typeof(CustomStringOption))
                value = reader.ReadInt32();
            else if (option.GetType() == typeof(CustomRoleOption))
            {
                value = reader.ReadInt32();
                value2 = reader.ReadInt32();
            }
            
            return new Data(option, value, value2);
        }

        public override void Handle(PlayerControl innerNetObject, Data data)
        {
            //PeasApi.Logger.LogInfo("4: " + data.Value.GetType());
            if (data.Option.GetType() == typeof(CustomToggleOption))
                ((CustomToggleOption) data.Option).SetValue((bool) data.Value);
            else if (data.Option.GetType() == typeof(CustomNumberOption))
                ((CustomNumberOption) data.Option).SetValue((float) data.Value);
            else if (data.Option.GetType() == typeof(CustomStringOption))
                ((CustomStringOption) data.Option).SetValue((int) data.Value);
            else if (data.Option.GetType() == typeof(CustomRoleOption))
                ((CustomRoleOption) data.Option).SetValue((int) data.Value,(int) data.Value2);
        }
    }
}