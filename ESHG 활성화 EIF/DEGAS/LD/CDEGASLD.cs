using LGCNS.ezControl.Common;
using LGCNS.ezControl.EIF.Solace;

using SolaceSystems.Solclient.Messaging;

using ESHG.EIF.FORM.COMMON;

namespace ESHG.EIF.FORM.DEGASLD
{
    public partial class CDEGASLD : CSolaceEIFServerBizRule
    {
        public IEIF_Biz EIF_Biz => (IEIF_Biz)Implement;

        #region Class Member variable
        public const string EQPTYPE = "DegasLoader";  //$ 2021.07.05 : Modeler Element의 Nick과 반드시 일치 시키시오!!
        #endregion

        #region FactovaLync Method Override
        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            #region Virtual Variable
            __INTERNAL_VARIABLE_STRING("V_W_EQP_ID", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "EQP ID");
            __INTERNAL_VARIABLE_STRING("V_W_SUBEQP_ID", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "Sub EQP ID(Degas)");

            __INTERNAL_VARIABLE_INTEGERLIST("V_W_SYSTEM_SYNC_TIME", "SYSTEM", enumAccessType.Virtual, 4, 0, 0, false, false, 0, string.Empty, "SYSTEM Sync Time");

            __INTERNAL_VARIABLE_INTEGER("V_TACTTIME_INTERVAL", "EQP_INFO", enumAccessType.Virtual, 0, 0, false, true, 60, "", "TactTime Interval(Sec) - 0: Not Use"); //$ 2022.08.01 : Tacttime 수집 주기

            __INTERNAL_VARIABLE_BOOLEAN("V_IS_NAK_TEST", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 무조건 Nak Reply, False - Nak Test 사용 안함");           //$ 2023.01.05 : Test Mode 추가
            __INTERNAL_VARIABLE_BOOLEAN("V_IS_TIMEOUT_TEST", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - 무조건 Time Out, False - Timeout Test 사용 안함");    //$ 2023.01.05 : Test Mode 추가

            __INTERNAL_VARIABLE_BOOLEAN("V_IS_ALMLOG_USE", "TESTMODE", enumAccessType.Virtual, true, false, false, "", "True - Alarm Set/Rest EIF, Biz Log 남김, False - Log 사용 안함");    //$ 2023.05.18 : Alarm Set/Rest Log 사용 유무

            __INTERNAL_VARIABLE_INTEGER("V_W_USE_TIME_SYNC", "SYSTEM", enumAccessType.Virtual, 24, 0, true, false, 0, string.Empty, "Data SYNC Time");  //2023.11.16 LWY : ESGM2 개발자 설정 시간동기화 추가 Virtual Var로 관리, 시간만 입력 가능 ( 1 ~ 23 )

            __INTERNAL_VARIABLE_SHORT("V_REMOTE_CMD", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.Virtual, 0, 0, true, true, 0, "", "Remote Command Send(1:RMS, 12: IT Bypass, 21: Pause");
            __INTERNAL_VARIABLE_SHORT("V_REMOTE_CMD", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.Virtual, 0, 0, true, true, 0, "", "Degas Remote Command Send(1:RMS, 12: IT Bypass, 21: Pause)");

            #region 적재기
            __INTERNAL_VARIABLE_SHORT("V_W_LOADER_WAIT_TIME", "STACK_DATA_INFO", enumAccessType.Virtual, 0, 0, true, true, 35, string.Empty, "LOADER WAIT COUNT");
            __INTERNAL_VARIABLE_SHORT("V_W_LOADER_BCR_READ_COUNT", "STACK_DATA_INFO", enumAccessType.Virtual, 0, 0, true, true, 3, string.Empty, "LOADER_BCR_READ_LIMIT_COUNT");
            #endregion
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

            #region 7.1.2.1 [C2-1] Equipment State Change Report_01 (Loader)
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4010", "DGS LD Equipment State");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SUBSTAT", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4012", "DGS LD Equipment SubState");

            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4011", "DGS LD Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_POPUP_DELAY_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4013", "DGS LD User Stop Pop-up Delay Time");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_TACT_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4020", "DGS LD EQP Tact Time");

            __INTERNAL_VARIABLE_STRING("I_W_CURRENT_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "DGS LD Current Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "DGS LD Current Group Lot ID");
            #endregion

            #region 7.1.2.1 [C2-1] Equipment State Change Report_02 (Degas)
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4014", "DGS Equipment State");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SUBSTAT", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4016", "DGS Equipment SubState");

            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4015", "DGS Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_POPUP_DELAY_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4017", "DGS User Stop Pop-up Delay Time");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_TACT_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4030", "DGS EQP Tact Time");

            //__INTERNAL_VARIABLE_STRING("I_W_CURRENT_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, false, true, string.Empty, "", "DGS Current Lot ID");
            //__INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, false, true, string.Empty, "", "DGS Current Group Lot ID");
            #endregion

            #region 7.1.2.2 [C2-2] Alarm Report_01
            //__INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Alarm ID");
            //__INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", CCategory.ALARM_RPT, enumAccessType.In, 0, 0, true, true, 0, "", "Equipment State");
            //__INTERNAL_VARIABLE_BOOLEAN("I_B_EQP_ALARM_TYPE", CCategory.ALARM_RP, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4008", "EQP Alarm Type");
            #endregion   

            #region 7.1.2.3 [C2-3] Host Alarm Message Send_01 (Loader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_ALARM_MSG_SEND", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3002", "DGS LD Host Alarm Msg Send");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_SEND_SYSTEM", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C00", "DGS LD Alarm Send System");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_PROC_STOP_TYPE", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C01", "DGS LD EQP Processing Stop Type");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_DISP_TYPE", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C02", "DGS LD Host Alarm Display Type");

            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_ALARM_ID", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C03", "DGS LD Host Alarm Code");
            __INTERNAL_VARIABLE_STRINGLIST("O_W_HOST_ALARM_MSG", "HOST_ALARM_MSG_SEND_01", enumAccessType.Out, 2, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=5C05,LENGTH=500, ENCODING=utf-16", "DGS LD Host Alarm Message");
            #endregion

            #region 7.1.2.3 [C2-3] Host Alarm Message Send_02 (Degas)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_ALARM_MSG_SEND", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3003", "DGS Host Alarm Msg Send");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_SEND_SYSTEM", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W,ADDRESS_NO=5D00", "DGS Alarm Send System");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_PROC_STOP_TYPE", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W,ADDRESS_NO=5D01", "DGS EQP Processing Stop Type");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_DISP_TYPE", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W,ADDRESS_NO=5D02", "DGS Host Alarm Display Type");

            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_ALARM_ID", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5D03", "DGS Host Alarm Code");
            __INTERNAL_VARIABLE_STRINGLIST("O_W_HOST_ALARM_MSG", "HOST_ALARM_MSG_SEND_02", enumAccessType.Out, 2, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5D05,LENGTH=500, ENCODING=utf-16", "DGS Host Alarm Message");
            #endregion

            #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report_01 (Loader)
            __INTERNAL_VARIABLE_BOOLEAN("I_B_AUTO_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4003", "DGS LD Auto Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_IT_BYPASS", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4004", "DGS LD IT Bypass");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REWORK_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4009", "DGS LD Rework Mode");
            __INTERNAL_VARIABLE_SHORT("I_W_HMI_LANG_TYPE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4001", "DGS LD HMI Language Type");
            #endregion

            #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report_02 (Degas)
            __INTERNAL_VARIABLE_BOOLEAN("I_B_AUTO_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40A3", "DGS Auto Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_IT_BYPASS", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40A4", "DGS IT Bypass");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REWORK_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40A9", "DGS Rework Mode");
            __INTERNAL_VARIABLE_SHORT("I_W_HMI_LANG_TYPE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4002", "DGS HMI Language Type");
            #endregion         

            #region 7.1.2.6 [C2-6] Remote Command Send_01 (Loader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_REMOTE_COMMAND_SEND", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=300E", "DGS LD Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("O_W_REMOTE_COMMAND_CODE", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=300E", "DGS LD Remote Command Code");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REMOTE_COMMAND_CONF", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=400E", "DGS LD Remote Command Confirm");
            __INTERNAL_VARIABLE_SHORT("I_W_REMOTE_COMMAND_CONF_ACK", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=400E", "DGS LD Remote Command Confirm ACK");
            #endregion

            #region 7.1.2.6 [C2-6] Remote Command Send_02 (Degas)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_REMOTE_COMMAND_SEND", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=309E", "DGS LD Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("O_W_REMOTE_COMMAND_CODE", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=309E", "DGS LD Remote Command Code");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REMOTE_COMMAND_CONF", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=409E", "DGS LD Remote Command Confirm");
            __INTERNAL_VARIABLE_SHORT("I_W_REMOTE_COMMAND_CONF_ACK", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=409E", "DGS LD Remote Command Confirm ACK");
            #endregion

            #region 7.1.2.8 [C2-8] Alarm Set Report_01 (Loader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_SET_CONF", $"{CCategory.ALARM_RPT}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D0", "DGS LD Alarm Set Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_SET_REQ", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D0", "DGS LD Alarm Set Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_SET_ID", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D0", "DGS LD Alarm Set ID");
            #endregion

            #region 7.1.2.8 [C2-8] Alarm Set Report_02 (Degas)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_SET_CONF", $"{CCategory.ALARM_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=30D2", "DGS Alarm Set Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_SET_REQ", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40D2", "DGS Alarm Set Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_SET_ID", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=40D2", "DGS Alarm Set ID");
            #endregion

            #region 7.1.2.9 [C2-9] Alarm Reset Report_01 (Loader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_RESET_CONF", $"{CCategory.ALARM_RPT}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D1", "DGS LD Alarm Reset Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_RESET_REQ", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D1", "DGS LD Alarm Reset Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_RESET_ID", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D1", "DGS LD Alarm Reset ID");
            #endregion

            #region 7.1.2.9 [C2-9] Alarm Reset Report_02 (Degas)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_RESET_CONF", $"{CCategory.ALARM_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=30D3", "DGS Alarm Reset Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_RESET_REQ", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=40D3", "DGS Alarm Reset Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_RESET_ID", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=40D3", "DGS Alarm Reset ID");
            #endregion

            #region 7.1.2.10 [C2-10] Smoke Alarm Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_EQP_SMOKE_STATUS_CONF", CCategory.SMOKE_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=300F", "Eqp Smoke Detect Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_SMOKE_DETECT_REQ", CCategory.SMOKE_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=400F", "Eqp Smoke Dectect Request");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SMOKE_STATUS", CCategory.SMOKE_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=400F", "Eqp Smoke Status");
            #endregion

            #region 7.2.2.1 [G2-1] Carrier ID Report
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3021", "TRAY BCR Read Req Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CARR_ID_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3021", "LD TrayID Request Confirm Ack");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=30F8,LENGTH=16", "LOTID");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3108,LENGTH=16", "Group LOTID");
            __INTERNAL_VARIABLE_STRING("O_W_PRODUCT_ID", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3100,LENGTH=16", "PRODUCTID");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_INFO", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=30E8,LENGTH=2", "SPECIAL_INFO");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_NO", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=30F0,LENGTH=10", "SPECIAL_INFO_NO");
            __INTERNAL_VARIABLE_SHORT("O_W_CHANNEL_CNT", CCategory.CARR_ID_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=316F", "Channel Count");
            __INTERNAL_VARIABLE_STRING("O_W_CELL_EXIST", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3170,LENGTH=100", "Cell Exist");
            __INTERNAL_VARIABLE_STRING("O_W_CELL_GRADE_LIST", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=31B0,LENGTH=100", "Cell Exist");
            __INTERNAL_VARIABLE_STRING("O_W_TRAY_LOTTYPE", CCategory.CARR_ID_RPT, enumAccessType.Out, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=316B,LENGTH=2", "Lot Type");
            __INTERNAL_VARIABLE_STRING("O_W_TRAY_LOTTYPE_USEFLAG", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=316C,LENGTH=2", "Tray Lot Type Use Flag");
            __INTERNAL_VARIABLE_STRING("O_W_LOADER_TRAY_HOST_INFO_YN", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=316D,LENGTH=2", "Tray GMES Info Y,N");
            
            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CARR_ID_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4021", "TRAY BCR Read Req");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", CCategory.CARR_ID_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4160,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", CCategory.CARR_ID_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4020", "Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", CCategory.CARR_ID_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Group Lot ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.CARR_ID_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4150", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.CARR_ID_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4150", "Reading Type");
            #endregion

            #region 7.2.2.2 [G2-2] Carrier Job Start Report
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CARR_JOB_START, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3022", "EQP Job Start Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CARR_JOB_START, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3022", "EQP Job Start Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CARR_JOB_START, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4022", "EQP Job Start");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", CCategory.CARR_JOB_START, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4160,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", CCategory.CARR_JOB_START, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4020", "Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", CCategory.CARR_JOB_START, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", CCategory.CARR_JOB_START, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Group Lot ID");            
            __INTERNAL_VARIABLE_STRING("I_W_PRODUCT_ID", CCategory.CARR_JOB_START, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4100,LENGTH=16", "PRODUCTID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.CARR_JOB_START, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4150", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.CARR_JOB_START, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4150", "Reading Type");
            #endregion

            #region 7.2.2.3 [G2-3] Carrier Output Report
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CARR_OUT_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3024", "EQP Job Complete Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CARR_OUT_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3024", "EQP Job Complete Confirm");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CARR_OUT_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4024", "EQP Job Complete");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4180,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", CCategory.CARR_OUT_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4023", "Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Group Lot ID");            
            __INTERNAL_VARIABLE_STRING("I_W_PRODUCT_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4100,LENGTH=16", "PRODUCTID");
            
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.CARR_OUT_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4150", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.CARR_OUT_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4150", "Reading Type");
            #endregion

            #region 7.3.5.3 [S7-5] Cell Traㅛ Transfer Info Request_01 (단 적재 정보 요청)
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3060", "Trying to Stack TRAY BCR Read Req Confirm");
            __INTERNAL_VARIABLE_SHORT("O_W_ACTION_CODE", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3060", "Stack ACTION CODE");

            __INTERNAL_VARIABLE_SHORT("O_W_STACK_COUNT", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3061", "Stack count");
            __INTERNAL_VARIABLE_SHORT("O_W_WAIT_TIME", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3062", "Stack waiting time");
            __INTERNAL_VARIABLE_SHORT("O_W_BCR_READ_COUNT", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3063", "Stack BCR Read Max Count");
            __INTERNAL_VARIABLE_BOOLEAN("O_B_TRAY_BCR_RETRY", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3061", "Trying to Stack TRAY BCR Read Retry");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4060", "LOADER Tray BCR Report");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4065,LENGTH=10", "Trying to Stack Tray ID");
            __INTERNAL_VARIABLE_STRING("I_W_STACKED_TRAY_ID", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4060,LENGTH=10", "Stacked Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4061", "LOADER Input Tray Exist");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_STACKED_TRAY_EXIST", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4062", "LOADER CARRIER Tray Exist");
            __INTERNAL_VARIABLE_SHORT("I_W_WAIT_TIME", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=406B", "Stack Wait Time");
            __INTERNAL_VARIABLE_SHORT("I_W_STACK_TRAY_COUNT", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=406A", "Stack Tray Count");
            #endregion

            #region 7.3.5.3 [S7-5] Cell Tray Transfer Info Request_02 (단 적재 배출 보고)
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=306A", "Stack Tray Output Req Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=306A", "Empty Tray Output Req Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=406A", "EMPTY Tray Output Req");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4065,LENGTH=10", "Trying to Stack Tray ID");
            __INTERNAL_VARIABLE_STRING("I_W_STACKED_TRAY_ID", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4060,LENGTH=10", "Stacked Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=406C", "EMPTY Loader Tray Exists");
            __INTERNAL_VARIABLE_SHORT("I_W_WAIT_TIME", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=406B", "Stack Wait Time");
            __INTERNAL_VARIABLE_SHORT("I_W_STACK_TRAY_COUNT", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=406A", "Stack Tray Count");
            #endregion

            #region 7.3.5.6 [S7-6]  LOT Change Report
            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.LOT_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=400A", "LOT Changing Req");
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