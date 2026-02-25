using System;
using LGCNS.ezControl.Common;
using LGCNS.ezControl.EIF.Solace;

using ESHG.EIF.FORM.COMMON;
using SolaceSystems.Solclient.Messaging;

namespace ESHG.EIF.FORM.EOLLD
{
    public partial class CEOLLD : CSolaceEIFServerBizRule
    {

        public IEIF_Biz EIF_Biz => (IEIF_Biz)Implement;

        #region Class Member variable
        public const string EQPTYPE = "EOLLoader";  //$ 2021.07.05 : Modeler Element의 Nick과 반드시 일치 시키시오!!
        #endregion

        #region FactovaLync Method Override
        protected override void DefineInternalVariable()
        {
            base.DefineInternalVariable();

            #region Virtual Variable
            __INTERNAL_VARIABLE_STRING("V_W_EQP_ID", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "EQP ID");
            __INTERNAL_VARIABLE_STRING("V_W_SUBEQP_ID", "EQP_INFO", enumAccessType.Virtual, false, true, string.Empty, string.Empty, "Sub EQP ID");
            __INTERNAL_VARIABLE_INTEGER("V_TACTTIME_INTERVAL", "EQP_INFO", enumAccessType.Virtual, 0, 0, false, true, 60, "", "TactTime Interval(Sec) - 0: Not Use"); //JH 2025.06.04 : Tacttime 수집 주기 카테고리를 "System"에 반영

            __INTERNAL_VARIABLE_SHORT("V_W_STACK_WAIT_TIME", "STACK_DATA_INFO", enumAccessType.Virtual, 0, 0, true, true, 10, string.Empty, "STACK WAIT TIME");
            __INTERNAL_VARIABLE_SHORT("V_W_STACK_BCR_READ_COUNT", "STACK_DATA_INFO", enumAccessType.Virtual, 0, 0, true, true, 3, string.Empty, "STACK BCR_READ_LIMIT_COUNT");

            __INTERNAL_VARIABLE_INTEGERLIST("V_W_SYSTEM_SYNC_TIME", "SYSTEM", enumAccessType.Virtual, 4, 0, 0, false, false, 0, string.Empty, "SYSTEM Sync Time");
            __INTERNAL_VARIABLE_INTEGER("V_W_USE_TIME_SYNC", "SYSTEM", enumAccessType.Virtual, 24, 0, true, false, 0, string.Empty, "Data SYNC Time");  //2023.11.16 LWY : ESGM2 개발자 설정 시간동기화 추가 Virtual Var로 관리, 시간만 입력 가능 ( 1 ~ 23 )

            __INTERNAL_VARIABLE_SHORT("V_REMOTE_CMD", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.Virtual, 0, 0, false, true, 0, "", "Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("V_REMOTE_CMD", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.Virtual, 0, 0, false, true, 0, "", "Remote Command Send2");

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

            #region 7.1.2.1 [C2-1] Equipment State Change Report_01 (Loader)
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4010", "Equipment State");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SUBSTAT", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4012", "Equipment SubState");

            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4011", "Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_POPUP_DELAY_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4013", "User Stop Pop-up Delay Time");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_TACT_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4020", "EQP Tact Time");


            __INTERNAL_VARIABLE_STRING("I_W_CURRENT_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "Current Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Current Group Lot ID");
            #endregion

            #region 7.1.2.1 [C2-1] Equipment State Change Report_02 (SSF)
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4014", "Equipment State");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SUBSTAT", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4016", "Equipment SubState");

            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4015", "Alarm ID");
            __INTERNAL_VARIABLE_SHORT("I_W_POPUP_DELAY_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4017", "User Stop Pop-up Delay Time");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_TACT_TIME", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4030", "EQP Tact Time");

            __INTERNAL_VARIABLE_STRING("I_W_CURRENT_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "Current Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.EQP_STAT_CHG_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Current Group Lot ID");
            #endregion

            #region 7.1.2.2 [C2-2] Alarm Report
            //__INTERNAL_VARIABLE_SHORT("I_W_ALARM_ID", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "", "Alarm ID");
            //__INTERNAL_VARIABLE_SHORT("I_W_EQP_STAT", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "", "Equipment State");
            //__INTERNAL_VARIABLE_BOOLEAN("I_B_EQP_ALARM_TYPE", CCategory.ALARM_RP, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4008", "EQP Alarm Type");
            #endregion

            #region 7.1.2.3 [C2-3] Host Alarm Message Send_01 (Loader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_ALARM_MSG_SEND", $"{CCategory.HOST_ALARM_MSG_SEND}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3002", "Host Alarm Msg Send");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_SEND_SYSTEM", $"{CCategory.HOST_ALARM_MSG_SEND}_01", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C00", "Alarm Send System");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_PROC_STOP_TYPE", $"{CCategory.HOST_ALARM_MSG_SEND}_01", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C01", "EQP Processing Stop Type");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_DISP_TYPE", $"{CCategory.HOST_ALARM_MSG_SEND}_01", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C02", "Host Alarm Display Type");

            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_ALARM_ID", $"{CCategory.HOST_ALARM_MSG_SEND}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=5C03", "Host Alarm Code");
            __INTERNAL_VARIABLE_STRINGLIST("O_W_HOST_ALARM_MSG", $"{CCategory.HOST_ALARM_MSG_SEND}_01", enumAccessType.Out, 2, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=5C05,LENGTH=500, ENCODING=utf-16", "Host Alarm Message");
            #endregion

            #region 7.1.2.3 [C2-3] Host Alarm Message Send_02 (SSF)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_HOST_ALARM_MSG_SEND", $"{CCategory.HOST_ALARM_MSG_SEND}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3003", "Host Alarm Msg Send");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_SEND_SYSTEM", $"{CCategory.HOST_ALARM_MSG_SEND}_02", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5D00", "Alarm Send System");
            __INTERNAL_VARIABLE_SHORT("O_W_EQP_PROC_STOP_TYPE", $"{CCategory.HOST_ALARM_MSG_SEND}_02", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5D01", "EQP Processing Stop Type");
            __INTERNAL_VARIABLE_SHORT("O_W_HOST_ALARM_DISP_TYPE", $"{CCategory.HOST_ALARM_MSG_SEND}_02", enumAccessType.Out, 0, 0, true, false, 0, "DEVICE_TYPE=W, ADDRESS_NO=5D02", "Host Alarm Display Type");

            __INTERNAL_VARIABLE_INTEGER("O_W_HOST_ALARM_ID", $"{CCategory.HOST_ALARM_MSG_SEND}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=5D03", "Host Alarm Code");
            __INTERNAL_VARIABLE_STRINGLIST("O_W_HOST_ALARM_MSG", $"{CCategory.HOST_ALARM_MSG_SEND}_02", enumAccessType.Out, 2, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=5D05,LENGTH=500, ENCODING=utf-16", "Host Alarm Message");
            #endregion

            #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report_01 (Loader)
            __INTERNAL_VARIABLE_BOOLEAN("I_B_AUTO_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4003", "Auto Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_IT_BYPASS", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4004", "IT Bypass");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REWORK_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4009", "Rework Mode");
            __INTERNAL_VARIABLE_SHORT("I_W_HMI_LANG_TYPE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4001", "HMI Language Type");
            #endregion

            #region 7.1.2.4 [C2-4] Equipment Operation Mode Change Report_02 (SSF)
            __INTERNAL_VARIABLE_BOOLEAN("I_B_AUTO_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40A3", "Auto Mode");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_IT_BYPASS", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40A4", "IT Bypass");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REWORK_MODE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40A9", "Rework Mode");
            __INTERNAL_VARIABLE_SHORT("I_W_HMI_LANG_TYPE", $"{CCategory.EQP_OP_MODE_CHG_RPT}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=4002", "HMI Language Type");
            #endregion

            #region 7.1.2.6 [C2-6] Remote Command Send_01 (Loader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_REMOTE_COMMAND_SEND", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=300E", "Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("O_W_REMOTE_COMMAND_CODE", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=300E", "Remote Command Code");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REMOTE_COMMAND_CONF", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=400E", "Remote Command Confirm");
            __INTERNAL_VARIABLE_SHORT("I_W_REMOTE_COMMAND_CONF_ACK", $"{CCategory.REMOTE_COMM_SND}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=400E", "Remote Command Confirm ACK");
            #endregion

            #region 7.1.2.6 [C2-6] Remote Command Send_02 (SSF)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_REMOTE_COMMAND_SEND", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=309E", "Remote Command Send");
            __INTERNAL_VARIABLE_SHORT("O_W_REMOTE_COMMAND_CODE", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=309E", "Remote Command Code");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_REMOTE_COMMAND_CONF", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=409E", "Remote Command Confirm");
            __INTERNAL_VARIABLE_SHORT("I_W_REMOTE_COMMAND_CONF_ACK", $"{CCategory.REMOTE_COMM_SND}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=409E", "Remote Command Confirm ACK");
            #endregion

            #region 7.1.2.8 [C2-8] Alarm Set Report_01 (Loader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_SET_CONF", $"{CCategory.ALARM_RPT}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D0", "Alarm Set Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_SET_REQ", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D0", "Alarm Set Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_SET_ID", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D0", "Alarm Set ID");
            #endregion

            #region 7.1.2.8 [C2-8] Alarm Set Report_02 (SSF)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_SET_CONF", $"{CCategory.ALARM_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D2", "Alarm Set Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_SET_REQ", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D2", "Alarm Set Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_SET_ID", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D2", "Alarm Set ID");
            #endregion

            #region 7.1.2.9 [C2-9] Alarm Reset Report_01 (Loader)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_RESET_CONF", $"{CCategory.ALARM_RPT}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D1", "Alarm Reset Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_RESET_REQ", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D1", "Alarm Reset Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_RESET_ID", $"{CCategory.ALARM_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D1", "Alarm Reset ID");
            #endregion

            #region 7.1.2.9 [C2-9] Alarm Reset Report_02 (SSF)
            __INTERNAL_VARIABLE_BOOLEAN("O_B_ALARM_RESET_CONF", $"{CCategory.ALARM_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=30D3", "Alarm Reset Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_ALARM_RESET_REQ", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=40D3", "Alarm Reset Request");
            __INTERNAL_VARIABLE_SHORT("I_W_ALARM_RESET_ID", $"{CCategory.ALARM_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=40D3", "Alarm Reset ID");
            #endregion

            #region 7.1.2.10 [C2-10] Smoke Alarm Report
            __INTERNAL_VARIABLE_BOOLEAN("O_B_EQP_SMOKE_STATUS_CONF", CCategory.SMOKE_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=300F", "Eqp Smoke Detect Confirm");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_SMOKE_DETECT_REQ", CCategory.SMOKE_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=400F", "Eqp Smoke Dectect Request");
            __INTERNAL_VARIABLE_SHORT("I_W_EQP_SMOKE_STATUS", CCategory.SMOKE_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=400F", "Eqp Smoke Status");
            #endregion

            #region 7.2.1.1 [G1-1]  Material Monitoring Data Report_01 (Loader)
            //__INTERNAL_VARIABLE_STRING("I_W_STAT_CHG_EVENT_CODE", $"{CCategory.MTRL_MONITER_DATA}_01", enumAccessType.In, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=55F0", "Material State Change Event Code(Loader)");
            #endregion

            #region 7.2.1.1 [G1-1]  Material Monitoring Data Report_02 (SSF)
            //__INTERNAL_VARIABLE_STRING("I_W_STAT_CHG_EVENT_CODE", $"{CCategory.MTRL_MONITER_DATA}_02", enumAccessType.In, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=55F4", "Material State Change Event Code(SSF)");
            #endregion

            #region 7.2.2.1 [G2-1] Carrier ID Report
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3021", "TRAY BCR Read Req Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CARR_ID_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3021", "LD TrayID Request Confirm Ack");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=30F8,LENGTH=16", "LOTID");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3108,LENGTH=16", "Group LOTID");
            __INTERNAL_VARIABLE_STRING("O_W_PRODUCT_ID", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3100,LENGTH=16", "PRODUCTID");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_INFO", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=30E8,LENGTH=2", "SPECIAL_INFO");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_NO", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=30F0,LENGTH=10", "SPECIAL_INFO_NO");
            __INTERNAL_VARIABLE_SHORT("O_W_CHANNEL_CNT", CCategory.CARR_ID_RPT, enumAccessType.Out, 0, 0, true, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=316F", "Channel Count");
            __INTERNAL_VARIABLE_STRING("O_W_CELL_EXIST", CCategory.CARR_ID_RPT, enumAccessType.Out, true, false, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=3170,LENGTH=100", "Cell Exist");
            __INTERNAL_VARIABLE_STRING("O_W_TRAY_LOTTYPE", CCategory.CARR_ID_RPT, enumAccessType.Out, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=316B,LENGTH=2", "Lot Type");
            __INTERNAL_VARIABLE_STRING("O_W_TRAY_LOTTYPE_USEFLAG", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=316C,LENGTH=2", "Tray Lot Type Use Flag");
            __INTERNAL_VARIABLE_STRING("O_W_LOADER_TRAY_HOST_INFO_YN", CCategory.CARR_ID_RPT, enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=316D,LENGTH=2", "Tray GMES Info Y,N");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CARR_ID_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4021", "TRAY BCR Read Req");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", CCategory.CARR_ID_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4160,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", CCategory.CARR_ID_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4020", "Tray Exist");
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
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", CCategory.CARR_JOB_START, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Group Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", CCategory.CARR_JOB_START, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "Lot ID");
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
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4108,LENGTH=16", "Group Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=40F8,LENGTH=16", "Lot ID");
            __INTERNAL_VARIABLE_STRING("I_W_PRODUCT_ID", CCategory.CARR_OUT_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W, ADDRESS_NO=4100,LENGTH=16", "PRODUCTID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.CARR_OUT_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=__G3_5_APD_RPT__I_B_TRIGGER_REPORTB,ADDRESS_NO=4150", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.CARR_OUT_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4150", "Reading Type");
            #endregion

            #region 7.2.3.5 [G3-5] Acutal Processing Data Report
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.APD_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3110", "Ocv Meas Cell Bcr Read Req Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.APD_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3110", "Ocv Meas Cell Bcr Read Req Confirm Ack");

            __INTERNAL_VARIABLE_SHORT("O_W_OCV_CELL_JUDG_REWORK", CCategory.APD_RPT, enumAccessType.Out, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3595", "Ocv Cell Judge Rework");
            __INTERNAL_VARIABLE_SHORT("O_W_OCV_MV_DAY_JUDG", CCategory.APD_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3596", "MV Day Judg");
            __INTERNAL_VARIABLE_INTEGER("O_W_OCV_MV_DAY_DATA", CCategory.APD_RPT, enumAccessType.Out, Int32.MaxValue, Int32.MinValue, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3597", "MV Day Data");
            __INTERNAL_VARIABLE_INTEGER("O_W_OCV_MV_DAY_SPEC_DATA", CCategory.APD_RPT, enumAccessType.Out, Int32.MaxValue, Int32.MinValue, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3625", "MV Day Spec Data"); //2021.09.21 New

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.APD_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4110", "Ocv Meas Cell BCR Read Req");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", CCategory.APD_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4100", "Ocv Meas Cell Exist");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", CCategory.APD_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4680,LENGTH=10", "Ocv Meas Cell ID");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", CCategory.APD_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46B0,LENGTH=16", "Ocv Meas Lot ID");
            __INTERNAL_VARIABLE_INTEGER("I_W_OCV_MEAS_VAL", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=5000", "Ocv Meas Value");
            __INTERNAL_VARIABLE_SHORT("I_W_OCV_MEAS_ACK", CCategory.APD_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=46F0", "Ocv Meas Ack(10 : OK, 11 : OCV NG, 12 : CRACK NG)");
            #endregion

            #region 7.2.3.5 [G3-5] Acutal Processing Data Report_02
            //2025.06.30 JMS : 장단변 APD 보고 추가 (EIF PI 정택철 책임님 사양 추가건)
            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.APD_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4111", "Cell Sealing Bcr Read Req");


            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.APD_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3111", "Cell Sealing Bcr Read Req Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.APD_RPT}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3111", "Cell Sealing Bcr Read Req Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", $"{CCategory.APD_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4102", "Ocv Meas Cell Exist");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4694,LENGTH=10", "Cell Sealing Cell ID");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=46C0,LENGTH=16", "Cell Sealing Lot ID");

            //CELL SEALING APD 목록(06-18)
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_001", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5020,LENGTH=10", "SEALING Cell ID");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_002", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=502A,LENGTH=2", "장변 실링 DGS 양극 두께 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_003", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=502B,LENGTH=2", "장변 실링 DGS 중앙 두께 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_004", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=502C,LENGTH=2", "장변 실링 DGS 음극 두께 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_005", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=502D,LENGTH=2", "장변 실링 DGS 양극 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_006", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=502E,LENGTH=2", "장변 실링 DGS 중앙 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_007", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=502F,LENGTH=2", "장변 실링 DGS 음극 폭 불량 JUDGE");

            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_008", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5036,LENGTH=2", "단변 실링 양극 PKG 두께 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_009", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5037,LENGTH=2", "단변 실링 양극 LEAD 두께 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_010", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5038,LENGTH=2", "단변 실링 양극 DGS 두께 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_011", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5039,LENGTH=2", "단변 실링 양극 PKG 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_012", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=503A,LENGTH=2", "단변 실링 양극 LEAD 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_013", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=503B,LENGTH=2", "단변 실링 양극 DGS 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_014", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=503C,LENGTH=2", "단변 실링 음극 PKG 두께 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_015", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=503D,LENGTH=2", "단변 실링 음극 LEAD 두께 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_016", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=503E,LENGTH=2", "단변 실링 음극 DGS 두께 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_017", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=503F,LENGTH=2", "단변 실링 음극 PKG 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_018", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5040,LENGTH=2", "단변 실링 음극 LEAD 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_019", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5041,LENGTH=2", "단변 실링 음극 DGS 폭 불량 JUDGE");

            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_020", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5042,LENGTH=2", "단변 실링 양극 PKG Edge 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_021", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5043,LENGTH=2", "단변 실링 양극 DGS Edge 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_022", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5044,LENGTH=2", "단변 실링 음극 PKG Edge 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_023", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5045,LENGTH=2", "단변 실링 음극 DGS Edge 폭 불량 JUDGE");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_024", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5033,LENGTH=2", "미사용 항목1");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_025", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5034,LENGTH=2", "미사용 항목2");
            __INTERNAL_VARIABLE_STRING("APD_CELL_DATA_026", $"{CCategory.APD_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=5035,LENGTH=2", "미사용 항목3");

            #endregion

            #region 7.2.4.2 [G4-2] Cell intput Report
            //2025.06.02 Cell 재투입 관련 사양추가 HG기준 반영하지 않지만 우선 관련 프러퍼티 추가 및 주석처리 진행
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CELL_IN_RPT, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3040", "Bad Cell SSF OutPut Req");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CELL_IN_RPT, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3040", "Cell SSF Output Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CELL_IN_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4040", "Bad Cell SSF OutPut Req");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", CCategory.CELL_IN_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4050", "Rework Cell Exists");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", CCategory.CELL_IN_RPT, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4630,LENGTH=20", "Rework Cell ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", CCategory.CELL_IN_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4159", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", CCategory.CELL_IN_RPT, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4159", "Reading Type");
            #endregion

            #region 7.2.4.3 [G4-3] Cell Output Report_01
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3045", "Bad Cell SSF OutPut Req");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3045", "Cell SSF Output Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4045", "Bad Cell SSF OutPut Req");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4055", "Bad Cell SSF OutPut Exists");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4640,LENGTH=20", "Bad Cell SSF Cell ID");
            __INTERNAL_VARIABLE_INTEGER("I_W_CELL_OUTPUT_INFO", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4678,LENGTH=1", "Bad Cell SSF OutPut Info");
            __INTERNAL_VARIABLE_INTEGER("I_W_CELL_OUTPUT_JUDG", $"{CCategory.CELL_OUT_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4670,LENGTH=1", "Bad Cell SSF Judge");
            #endregion 

            #region 7.2.4.3 [G4-3] Cell Output Report_02
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=3046", "Bad Cell SSF OutPut Req");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=3046", "Cell Taping Output Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4046", "Bad Cell SSF OutPut Req");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4056", "Bad Cell SSF OutPut Exists");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=464A,LENGTH=20", "Bad Cell Taping Cell ID");
            __INTERNAL_VARIABLE_INTEGER("I_W_CELL_OUTPUT_INFO", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4679,LENGTH=1", "Bad Cell Taping OutPut Info");
            __INTERNAL_VARIABLE_INTEGER("I_W_CELL_OUTPUT_JUDG", $"{CCategory.CELL_OUT_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4671,LENGTH=1", "Bad Cell Taping Judge");
            #endregion

            #region 7.2.4.6 [G4-6] Cell Information Requeset
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, CCategory.CELL_INFO_REQ, enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=304A", "Ocv Input Cell BCR Read Req");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=303A", "Ocv Input Cell Bcr Read Req Confirm Ack");

            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID", CCategory.CELL_INFO_REQ, enumAccessType.Out, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3550,LENGTH=16", "Tray Lot ID");
            //$ 2024.12.09 |tlsrmsdl1| : O_W_GROUP_LOT_ID - Counter Spec 기준 '8WORD' 이기때문에 LENGTH = 10 > 16 변경
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID", CCategory.CELL_INFO_REQ, enumAccessType.Out, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3558,LENGTH=16", "Tray Group Lot ID");
            __INTERNAL_VARIABLE_STRING("O_W_PRODUCT_ID", CCategory.CELL_INFO_REQ, enumAccessType.Out, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3560,LENGTH=16", "Product CD");
            __INTERNAL_VARIABLE_STRING("O_W_MODEL_ID", CCategory.CELL_INFO_REQ, enumAccessType.Out, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3568,LENGTH=4", "Model ID");
            __INTERNAL_VARIABLE_SHORT("O_W_CHECKITEM", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=356A,LENGTH=2", "Check Item");

            __INTERNAL_VARIABLE_INTEGER("O_W_OCV_SPEC_MAX", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3574", "Ocv Spec Max");
            __INTERNAL_VARIABLE_INTEGER("O_W_OCV_SPEC_MIN", CCategory.CELL_INFO_REQ, enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3576", "Ocv Spec Min");
            __INTERNAL_VARIABLE_STRING("O_W_OCV_CELL_GRADE", CCategory.CELL_INFO_REQ, enumAccessType.Out, true, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3578,LENGTH=2", "Ocv Cell Grade");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, CCategory.CELL_INFO_REQ, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=404A", "Ocv Input Cell BCR Read Req");

            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST", CCategory.CELL_INFO_REQ, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=405A", "Ocv Input Cell Exist");
            __INTERNAL_VARIABLE_STRING("I_W__CELL_ID", CCategory.CELL_INFO_REQ, enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4550,LENGTH=10", "Ocv Input Cell ID, G4_6_CELL_INFO_REQ:I_W_CELL_ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_BCR_USE", CCategory.CELL_INFO_REQ, enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4158", "Ocv Input Cell BCR Use (0:BCR Not Use, 1:BCR Use)");
            __INTERNAL_VARIABLE_INTEGER("I_W_CELL_BCR_READ_TYPE", CCategory.CELL_INFO_REQ, enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4158", "Ocv Input Bcr Read Type (1:RFID, 2:BCR, 3:Key In)");

            #endregion

            #region 7.3.5.3 [S7-5] Cell Trau Transfer Info Request_01 (단 적재 정보 요청)

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
            __INTERNAL_VARIABLE_SHORT("I_W_STACK_TRAY_COUNT", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=406A", "Stack BCR Read Count");

            #endregion

            #region 7.3.5.3 [S7-5] Cell Tray Transfer Info Request_02 (단 적재 배출 보고)

            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B, ADDRESS_NO=306A", "Stack Tray Output Req Confirm");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W, ADDRESS_NO=306A", "Empty Tray Output Req Confirm Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=406A", "EMPTY Tray Output Req");

            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4065,LENGTH=10", "Trying to Stack Tray ID");
            __INTERNAL_VARIABLE_STRING("I_W_STACKED_TRAY_ID", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4060,LENGTH=10", "Stacked Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=406C", "EMPTY Loader Tray Exists");
            __INTERNAL_VARIABLE_SHORT("I_W_WAIT_TIME", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=406B", "Stack Wait Time");
            __INTERNAL_VARIABLE_SHORT("I_W_STACK_TRAY_COUNT", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=406A", "Stack Tray Count"); //JH 2025.06.05 (TODO) 사양서에는 적재단수 인데 적재요청의 BCR READING 횟수와 ADDRESS가 겹침-> 사양수정 필요해 보임

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