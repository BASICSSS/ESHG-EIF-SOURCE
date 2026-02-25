using LGCNS.ezControl.Common;
using LGCNS.ezControl.EIF.Solace;

using SolaceSystems.Solclient.Messaging;


namespace ESHG.EIF.FORM.EOLPACKER
{
    public partial class CPACKER : CSolaceEIFServerBizRule
    {
        public IEIF_Biz EIF_Biz => (IEIF_Biz)Implement;

        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            // BASIC INFO
            __INTERNAL_VARIABLE_BOOLEAN("V_GMES_EQP_MODIFY_01", "BASICINFO", enumAccessType.Virtual, false, false, false, string.Empty, "Interlock Use");
            __INTERNAL_VARIABLE_BOOLEAN("V_GMES_EQP_MODIFY_02", "BASICINFO", enumAccessType.Virtual, false, false, false, string.Empty, "Host Alarm Use");
            __INTERNAL_VARIABLE_BOOLEAN("V_GMES_EQP_MODIFY_03", "BASICINFO", enumAccessType.Virtual, false, false, false, string.Empty, "Test Permit Use");
            __INTERNAL_VARIABLE_BOOLEAN("V_GMES_EQP_MODIFY_04", "BASICINFO", enumAccessType.Virtual, false, false, false, string.Empty, "Eqp Status Use");

            __INTERNAL_VARIABLE_INTEGER("V_HOST_ALARM_MSG_SET_INTERVAL", "BASICINFO", enumAccessType.Virtual, 0, 0, false, false, 24, string.Empty, "Host Alarm Message Set Interval(Hr)");

            __INTERNAL_VARIABLE_STRING("V_LANGUAGE_ID", "BASICINFO", enumAccessType.Virtual, false, false, "ko-KR", string.Empty, "en-US/ko-KR/zh-CN");

            __INTERNAL_VARIABLE_STRING("V_FACTORY_CODE", "BASICINFO", enumAccessType.Virtual, false, false, string.Empty, string.Empty, string.Empty);
            __INTERNAL_VARIABLE_STRING("V_SITE_CODE", "BASICINFO", enumAccessType.Virtual, false, false, "", string.Empty, string.Empty);
            __INTERNAL_VARIABLE_STRING("V_SHOP_ID", "BASICINFO", enumAccessType.Virtual, false, false, "", string.Empty, string.Empty);
            __INTERNAL_VARIABLE_STRING("V_EQP_ID_01", "BASICINFO", enumAccessType.Virtual, false, false, string.Empty, string.Empty, string.Empty);
            __INTERNAL_VARIABLE_STRING("V_PROCESS_ID_01", "BASICINFO", enumAccessType.Virtual, false, false, string.Empty, string.Empty, string.Empty);

            __INTERNAL_VARIABLE_BOOLEAN("V_RMS_3_USE", "ADDINFO", enumAccessType.Virtual, false, false, false, string.Empty, "RMS 3 Level Use");
            __INTERNAL_VARIABLE_BOOLEAN("V_RMS_1_5_USE", "ADDINFO", enumAccessType.Virtual, false, false, false, string.Empty, "RMS 1.5 Level Use");

            __INTERNAL_VARIABLE_STRING("V_MAIN_EQP_ID", "ADDINFO", enumAccessType.Virtual, false, false, string.Empty, string.Empty, "Main Equipment ID for Eqp Loss");
            __INTERNAL_VARIABLE_STRING("V_HMI_EQP", "ADDINFO", enumAccessType.Virtual, false, false, string.Empty, string.Empty, "HMI Equipment Type : GOT2000 or PRO-FACE");

            __INTERNAL_VARIABLE_STRING("V_RMS_CONNECTION_INFO", "BASICINFO", enumAccessType.Virtual, false, false, string.Empty, string.Empty, "RMS CONNECTION INFO, 현재 접속된 RMS 서버정보를 보여주는 변수(NO Write)");
        }

        protected override void OnMessageReceived(IMessage request, string topic, string message)
        {
            EIF_Biz.OnMessageReceived(request, topic, message);
        }
    }

    public interface IEIF_Biz
    {
        void OnMessageReceived(IMessage request, string topic, string message);
    }
}