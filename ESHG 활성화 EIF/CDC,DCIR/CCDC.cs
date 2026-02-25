using System.Collections.Generic;

using LGCNS.ezControl.Common;
using LGCNS.ezControl.Driver.Serial;
using LGCNS.ezControl.EIF.Solace;

namespace ESHG.EIF.FORM.CDC
{
    public partial class CCDC : CSolaceEIFSocketServerBizRule
    {
        public IEIF_Biz EIF_Biz => (IEIF_Biz)Implement;

        public Dictionary<string, string> DicClientEqpID = null; //Connection Client의 EQP ID 정보 Implement에 선언 시 값이 초기화 되어 Base에 선언

        #region Varialbe Define
        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            __INTERNAL_VARIABLE_STRING("V_HOST_OBJECT_ID", "BASICINFO", enumAccessType.Virtual, false, true, "0000000000", "", "HOST_OBJECT_ID");

            //$ 2018.12.02 : Fatory Lync Modeler에 신규 Virtual 변수 추가
            __INTERNAL_VARIABLE_INTEGER("V_WAITTIME", "EQPINFO", enumAccessType.Virtual, 0, 0, false, true, 60000, "", "EERR Request Wait Time(msec)");

            __INTERNAL_VARIABLE_BOOLEAN("V_IS_NAK_TEST", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 무조건 Nak Reply, False - Nak Test 사용 안함");           //$ 2023.01.05 : Test Mode 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_TIMEOUT_TEST", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 무조건 Time Out, False - Timeout Test 사용 안함");    //$ 2023.01.05 : Test Mode 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_TEMPLOG_USE", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 온도 Log 남김, False - 온도 Log  사용 안함");    //$ 2023.06.03 : Log 사용 여부 추가
        }

        protected override void OnInstancingCompleted()
        {
            base.OnInstancingCompleted();

            this.DicClientEqpID = new Dictionary<string, string>();
        }

        #endregion

        protected override void OnClientConnected(CSocketClient client)
        {
            base.OnClientConnected(client);

            EIF_Biz.OnConnected(client);
        }

        protected override void OnClientDisconnected(CSocketClient client)
        {
            base.OnClientDisconnected(client);

            EIF_Biz.OnDisconnect(client);
        }

        protected override void ClientReceivedBytes(CSocketClient client, byte[] packet)
        {
            base.ClientReceivedBytes(client, packet);

            EIF_Biz.OnClientReceivedBytes(client, packet);
        }
    }

    public interface IEIF_Biz
    {
        void OnConnected(CSocketClient client);

        void OnDisconnect(CSocketClient client);

        void OnClientReceivedBytes(CSocketClient client, byte[] packet);
    }
}
