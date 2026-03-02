using System;

using LGCNS.ezControl.Solace;
using SolaceSystems.Solclient.Messaging;

namespace ESHG.EIF.FORM.UTIL
{
    public class SolaceElement : CSolaceDevice
    {
        public event EventHandler<SolaceMessageReceivedEventArgs> OnSolaceMessageReceived = null;

        protected override void OnInitializeCompleted()
        {
            base.OnInitializeCompleted();
        }


        protected override void OnMessageReceived(IMessage request, string topic, string message)
        {
            base.OnMessageReceived(request, topic, message);

            if (OnSolaceMessageReceived != null)
            {
                OnSolaceMessageReceived(this, new SolaceMessageReceivedEventArgs(request, topic, message));
            }
        }

    }

    public class SolaceMessageReceivedEventArgs : EventArgs
    {
        internal SolaceMessageReceivedEventArgs(IMessage request, string topic, string message)
        {
            this.Request = request;
            this.Topic = topic;
            this.Message = message;
        }

        public IMessage Request
        {
            get;
            internal set;
        }
        public string Topic
        {
            get;
            internal set;
        }
        public string Message
        {
            get;
            internal set;
        }
    }
}