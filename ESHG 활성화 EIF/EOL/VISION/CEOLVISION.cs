using LGCNS.ezControl.Common;
using LGCNS.ezControl.EIF.Solace;

using ESHG.EIF.FORM.COMMON;
using SolaceSystems.Solclient.Messaging;

namespace ESHG.EIF.FORM.EOLVISION
{
    public partial class CEOLVISION : CSolaceEIFServerBizRule
    {

        public IEIF_Biz EIF_Biz => (IEIF_Biz)Implement;

        #region Class Member variable
        public const string EQPTYPE = "EOLVision";  //$ 2021.07.05 : Modeler Element의 Nick과 반드시 일치 시키시오!!
        #endregion

        #region FactovaLync Method Override
        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            #region Virtual Variable
            __INTERNAL_VARIABLE_STRING("V_W_EQP_ID", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "EQP ID");
            __INTERNAL_VARIABLE_INTEGER("V_TACTTIME_INTERVAL", "EQP_INFO", enumAccessType.Virtual, 0, 0, false, true, 60, "", "TactTime Interval(Sec) - 0: Not Use"); //$ 2022.08.01 : Tacttime 수집 주기 

            __INTERNAL_VARIABLE_INTEGERLIST("V_W_SYSTEM_SYNC_TIME", "SYSTEM", enumAccessType.Virtual, 4, 0, 0, false, false, 0, string.Empty, "SYSTEM Sync Time");
            __INTERNAL_VARIABLE_INTEGER("V_W_USE_TIME_SYNC", "SYSTEM", enumAccessType.Virtual, 24, 0, true, false, 0, string.Empty, "Data SYNC Time");  //2023.11.16 LWY : ESGM2 개발자 설정 시간동기화 추가 Virtual Var로 관리, 시간만 입력 가능 ( 1 ~ 23 )

            __INTERNAL_VARIABLE_SHORT("V_REMOTE_CMD", CCategory.REMOTE_COMM_SND, enumAccessType.Virtual, 0, 0, false, true, 0, "", "Remote Command Send");

            __INTERNAL_VARIABLE_BOOLEAN("V_IS_NAK_TEST", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 무조건 Nak Reply, False - Nak Test 사용 안함");           //$ 2023.01.05 : Test Mode 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_TIMEOUT_TEST", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 무조건 Time Out, False - Timeout Test 사용 안함");    //$ 2023.01.05 : Test Mode 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_ALMLOG_USE", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - Alarm Set/Rest EIF, Biz Log 남김, False - Log 사용 안함");    //$ 2023.05.18 : Alarm Set/Rest Log 사용 유무           
            #endregion

            #region 7.1.1.1 [C1-1] EQP Communication Check
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_COMM_CHK", CCategory.EQP_COMM_CHK, enumAccessType.In, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=4000", "EQP Comm Check");
            #endregion

            #region 7.1.1.2 [C1-2] Host Communication Check 
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_COMM_CHK", CCategory.HOST_COMM_CHK, enumAccessType.Out, false, false, false, "DEVICE_TYPE=B, ADDRESS_NO=3000", "Host Comm Check");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_HOST_COMM_CHK_CONF", CCategory.HOST_COMM_CHK, enumAccessType.In, true, false, false, "DEVICE_TYPE=B, ADDRESS_NO=4000", "Host Comm Check Confirm");
            #endregion

            #region 7.1.1.3 [C1-3] Communication State Change Report
            __INTERNAL_VARIABLE_BOOLEAN("I_B_COMM_ON", CCategory.COMM_STAT_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4001", "COMM ON");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_COMM_OFF", CCategory.COMM_STAT_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4002", "COMM OFF");
            #endregion

            #region 7.1.1.4 [C1-4] Date and Time Set Request
            __INTERNAL_VARIABLE_BOOLEAN("O_B_DATE_TIME_SET_REQ", CCategory.DATE_TIME_SET_REQ, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3001", "Date Time Set Request");
            __INTERNAL_VARIABLE_SHORTLIST("O_W_DATE_TIME", CCategory.DATE_TIME_SET_REQ, enumAccessType.Out, 6, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3000", "Date and Time");
            #endregion

            #region 7.1.2.1 [C2-1] Equipment State Change Report
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4010", "Equipment State");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SUBSTAT", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4012", "Equipment SubState");

            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4011", "Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_POPUP_DELAY_TIME", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4013", "User Stop Pop-up Delay Time");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_TACT_TIME", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4040", "EQP Tact Time");

            __INTERNAL_VARIABLE_STRING("I_W_CURRENT_LOT_ID", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "Current Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", CCategory.EQP_STAT_CHG_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Current Group Lot ID");
            #endregion

            #region 7.1.2.2 [C2-2] Alarm Report
            //__INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Alarm ID");
            //__INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Equipment State");
            //__INTERNAL_VARIABLE_BOOLEAN("I_B_EQP_ALARM_TYPE", CCategory.ALARM_RP, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4008", "EQP Alarm Type");
            #endregion

            #region 7.1.2.3 [C2-3] Host Alarm Message Send
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_ALARM_MSG_SEND", "HOST_ALARM_MSG_SEND", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3002", "Host Alarm Msg Send");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_SEND_SYSTEM", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C00", "Alarm Send System");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_PROC_STOP_TYPE", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C01", "EQP Processing Stop Type");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_DISP_TYPE", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C02", "Host Alarm Display Type");

            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_ALARM_ID", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C03", "Host Alarm Code");
            __INTERNAL_VARIABLE_STRINGLIST("O_W_HOST_ALARM_MSG", "HOST_ALARM_MSG_SEND", enumAccessType.Out, 2, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=5C05,LENGTH=500, ENCODING=utf-16", "Host Alarm Message");
            #endregion

            #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report
            __INTERNAL_VARIABLE_BOOLEAN("I_B_AUTO_MODE", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4003", "Auto Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_IT_BYPASS", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4004", "IT Bypass");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REWORK_MODE", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4009", "Rework Mode");
            __INTERNAL_VARIABLE_SHORT("I_W_HMI_LANG_TYPE", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4001", "HMI Language Type");
            #endregion

            #region 7.1.2.6 [C2-6] Remote Command Send
            __INTERNAL_VARIABLE_BOOLEAN("O_B_REMOTE_COMMAND_SEND", CCategory.REMOTE_COMM_SND, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=300E", "Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("O_W_REMOTE_COMMAND_CODE", CCategory.REMOTE_COMM_SND, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=300E", "Remote Command Code");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REMOTE_COMMAND_CONF", CCategory.REMOTE_COMM_SND, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=400E", "Remote Command Confirm");
            __INTERNAL_VARIABLE_SHORT("I_W_REMOTE_COMMAND_CONF_ACK", CCategory.REMOTE_COMM_SND, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=400E", "Remote Command Confirm ACK");
            #endregion

            #region 7.1.2.8 [C2-8] Alarm Set Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_SET_CONF", CCategory.ALARM_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D0", "Alarm Set Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_SET_REQ", CCategory.ALARM_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D0", "Alarm Set Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_SET_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D0", "Alarm Set ID");
            #endregion

            #region 7.1.2.9 [C2-9] Alarm Reset Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_RESET_CONF", CCategory.ALARM_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D1", "Alarm Reset Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_RESET_REQ", CCategory.ALARM_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D1", "Alarm Reset Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_RESET_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D1", "Alarm Reset ID");
            #endregion

            #region 7.1.2.10 [C2-10] Smoke Alarm Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_EQP_SMOKE_STATUS_CONF", CCategory.SMOKE_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=300F", "Eqp Smoke Detect Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_SMOKE_DETECT_REQ", CCategory.SMOKE_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=400F", "Eqp Smoke Dectect Request");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SMOKE_STATUS", CCategory.SMOKE_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=400F", "Eqp Smoke Status");
            #endregion

            #region 7.2.4.6 [G4-6] Cell Information Requeset
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CELL_INFO_REQ, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=304A", "Cell Info Req Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=304A", "Cell Info Req Ack");

            //2025.06.19 JMS : 사양서대로 IN->OUT으로 수정
            __INTERNAL_VARIABLE_STRING("O_W_PRODUCT_ID", CCategory.CELL_INFO_REQ, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3560,LENGTH=16", "Product ID");
            __INTERNAL_VARIABLE_STRING("O_W_PKG_LOT_ID", CCategory.CELL_INFO_REQ, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3558,LENGTH=16", "Package Lot ID");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_TYPE", CCategory.CELL_INFO_REQ, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3540,LENGTH=2", "Cell Lot Type");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CELL_INFO_REQ, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=404A", "Cell Info Req");

            __INTERNAL_VARIABLE_STRING("I_W_MODEL_ID", CCategory.CELL_INFO_REQ, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4540,LENGTH=16", "Model ID");
            __INTERNAL_VARIABLE_STRING("I_W_LINE_ID", CCategory.CELL_INFO_REQ, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4548,LENGTH=16", "Line ID");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", CCategory.CELL_INFO_REQ, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4550,LENGTH=20", "Cell ID");

            #endregion
        }
        #endregion

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