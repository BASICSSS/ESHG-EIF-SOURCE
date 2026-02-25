using LGCNS.ezControl.Common;
using LGCNS.ezControl.EIF.Solace;

using ESHG.EIF.FORM.COMMON;
using SolaceSystems.Solclient.Messaging;

namespace ESHG.EIF.FORM.JIGLDULD
{
    public partial class CJIGLDULD : CSolaceEIFServerBizRule
    {
        public IEIF_Biz EIF_Biz => (IEIF_Biz)Implement;

        #region Class Member variable
        public const string EQPTYPE = "JIGLDULD";  //$ 2021.07.05 : Modeler Element의 Nick과 반드시 일치 시키시오!!
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

            __INTERNAL_VARIABLE_SHORT("V_TRAYINCELLCNT", "EQP_INFO", enumAccessType.Virtual, 0, 0, false, true, 0, "", "Tray Max Cell Count ex) 36, 30, 72");           //$ 2021.12.15 : ESWA4 대응을 위해 추가
            __INTERNAL_VARIABLE_SHORT("V_PNPACTCELLCNT", "EQP_INFO", enumAccessType.Virtual, 0, 0, false, true, 0, "", "P&P Action Cell Count ex) 1P&P, 2P&P, 3P&P");   //$ 2021.12.15 : ESWA4 대응을 위해 추가
            __INTERNAL_VARIABLE_SHORT("V_W_LOADER_WAIT_TIME", "STACK_TRAY_INFO", enumAccessType.Virtual, 0, 0, true, true, 10, string.Empty, "LOADER WAIT COUNT");
            __INTERNAL_VARIABLE_SHORT("V_W_LOADER_BCR_READ_COUNT", "STACK_TRAY_INFO", enumAccessType.Virtual, 0, 0, true, true, 3, string.Empty, "LOADER_BCR_READ_LIMIT_COUNT");
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
            //__INTERNAL_VARIABLE_BOOLEAN("I_B_REWORK_MODE", CCategory.EQP_OP_MODE_CHG_RPT, enumAccessType.In, true, true, false, "DEVICE_TYPE=B, ADDRESS_NO=4009", "Rework Mode");
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

            #region 7.2.2.1 [G2-1] Carrier ID Report - Loc101
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3021", "Loc101 Carrier(Tray) ID Reading Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3021", "Loc101 Carrier(Tray) ID Reading Ack");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=30F8,LENGTH=16", "LOTID");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3108,LENGTH=10", "Group LOTID");
            __INTERNAL_VARIABLE_STRING("O_W_PRODUCT_ID", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3100,LENGTH=16", "PRODUCTID");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_INFO", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=30E8,LENGTH=1", "SPECIAL_INFO");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_NO", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=30F0,LENGTH=10", "SPECIAL_INFO_NO");
            __INTERNAL_VARIABLE_STRING("O_W_ROUTE_ID", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=30E0,LENGTH=16", "ROUTE ID");
            __INTERNAL_VARIABLE_STRING("O_W_CELL_SIZE", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3164,LENGTH=10", "CELL SIZE");
            __INTERNAL_VARIABLE_SHORT("O_W_CHANNEL_CNT", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3169", "Channel Count");
            __INTERNAL_VARIABLE_SHORT("O_W_CELL_CNT", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=316A", "CELL Count");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_TYPE", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=316B,LENGTH=1", "Lot Type");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CARR_ID_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4021", "Loc101 Carrier(Tray) ID Reading");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4160,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4020", "Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4108,LENGTH=10", "Group LOTID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BUFFER_EMPTY_YN", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4030", "Buffer Empty");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4050", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.CARR_ID_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4050", "Reading Type");

            #endregion

            #region 7.2.2.1 [G2-1] Carrier ID Report_02 - Loc102
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CARR_ID_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3026", "Loc102 Carrier(Tray) ID Reading Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CARR_ID_RPT}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3026", "Loc102 Carrier(Tray) ID Reading Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CARR_ID_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4026", "Loc102 Carrier(Tray) ID Reading");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID_LOWER", $"{CCategory.CARR_ID_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4A36,LENGTH=10", "Tray ID Lower");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID_UPPER", $"{CCategory.CARR_ID_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4A3B,LENGTH=10", "Tray ID Upper");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.CARR_ID_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4142", "Tray Exist");
            __INTERNAL_VARIABLE_SHORT("I_W_TRAY_CARRY_CNT", $"{CCategory.CARR_ID_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A35", "Tray Step");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.CARR_ID_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4051", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.CARR_ID_RPT}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4051", "Reading Type");
            #endregion

            #region 7.2.2.1 [G2-1] Carrier ID Report_03 Port104
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=302B", "Port104 Carrier(Tray) ID Reading Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=302B", "Port104 Carrier(Tray) ID Reading Ack");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID_1ST", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=37A8,LENGTH=10", "LOTID 1st");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID_1ST", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=37B0,LENGTH=10", "Group LOTID 1st");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_INFO_1ST", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3798,LENGTH=1", "SPECIAL_INFO 1st");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_NO_1ST", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=37A0,LENGTH=10", "SPECIAL_INFO_NO 1st");
            __INTERNAL_VARIABLE_STRING("O_W_ROUTE_ID_1ST", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3790,LENGTH=16", "ROUTE ID 1st");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID_2ND", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=37D0,LENGTH=10", "LOTID 2nd");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID_2ND", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=37D8,LENGTH=10", "Group LOTID 2nd");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_INFO_2ND", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=37C0,LENGTH=1", "SPECIAL_INFO 2nd");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_NO_2ND", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=37C8,LENGTH=10", "SPECIAL_INFO_NO 2nd");
            __INTERNAL_VARIABLE_STRING("O_W_ROUTE_ID_2ND", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=37B8,LENGTH=16", "ROUTE ID 2nd");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CARR_ID_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=402B", "Port104 Carrier(Tray) ID Reading");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID_LOWER", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4A96,LENGTH=10", "Tray ID Lower");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID_UPPER", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4A9B,LENGTH=10", "Tray ID Upper");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4146", "Tray Exist");
            __INTERNAL_VARIABLE_SHORT("I_W_TRAY_CARRY_CNT", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A95", "Tray Step");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4052", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.CARR_ID_RPT}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4052", "Reading Type");
            #endregion

            #region 7.2.2.2 [G2-2] Carrier Input Report - Loc101
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CARR_JOB_START}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3022", "Loc101 Carrier(Tray) Job Start Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CARR_JOB_START}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3022", "Loc101 Carrier(Tray) Job Start Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CARR_JOB_START}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4022", "Loc101 Carrier(Tray) Job Start");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", $"{CCategory.CARR_JOB_START}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4160,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.CARR_JOB_START}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4020", "Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", $"{CCategory.CARR_JOB_START}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=40F8,LENGTH=16", "LOTID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.CARR_JOB_START}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4108,LENGTH=10", "Group LOTID");
            __INTERNAL_VARIABLE_STRING("I_W_PRODUCT_ID", $"{CCategory.CARR_JOB_START}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4100,LENGTH=16", "PRODUCTID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.CARR_JOB_START}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4050", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.CARR_JOB_START}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4050", "Reading Type");

            #endregion

            #region 7.2.2.2 [G2-2] Carrier Input Report_02 - Loc202
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3027", "Loc202 Carrier(Tray) Job Start Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3027", "Loc202 Carrier(Tray) Job Start Ack");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID", $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3758,LENGTH=16", "LOTID");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID", $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3760,LENGTH=10", "Group LOTID");
            __INTERNAL_VARIABLE_STRING("O_W_ROUTE_ID", $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3740,LENGTH=16", "ROUTE ID");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_INFO", $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3748,LENGTH=1", "SPECIAL_INFO");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_NO", $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3750,LENGTH=10", "SPECIAL_INFO_NO");
            __INTERNAL_VARIABLE_STRING("O_W_PRODUCT_ID", $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=37E0,LENGTH=16", "PRODUCTID");
            //__INTERNAL_VARIABLE_SHORT("O_W_ULD_BUFFER_CHANNEL_CNT", "LD_TRAY", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=316F", "ULD Buffer Channel Count");
            __INTERNAL_VARIABLE_STRING("O_W_CELL_EXIST_LIST", $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3170,LENGTH=40", "CELL EXIST LIST");
            __INTERNAL_VARIABLE_STRING("O_W_CELL_GRADE_LIST", $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=31B0,LENGTH=40", "CELL GRADE LIST");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_TYPE", $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=31E2,LENGTH=1", "Lot Type");
            // 2025.06.02 사양서에 없는 내용으로 임시 삭제
            //__INTERNAL_VARIABLE_STRING("O_W_FORCE_OUT_FLAG", $"{CCategory.CARR_JOB_START}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=31E4,LENGTH=1", "ULD Tray Force Out Flag");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CARR_JOB_START}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4027", "Loc202 Carrier(Tray) Job Start");
            __INTERNAL_VARIABLE_STRING("I_W_JIG_ID", $"{CCategory.CARR_JOB_START}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4165,LENGTH=10", "Buffer Jig ID");

            #endregion

            #region 7.2.2.2 [G2-2] Carrier Input Report_03 - Loc104
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CARR_JOB_START}_03", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=302C", "Loc104 Carrier(Tray) Job Start Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CARR_JOB_START}_03", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=302C", "Loc104 Carrier(Tray) Job Start Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CARR_JOB_START}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=402C", "Loc104 Carrier(Tray) Job Start");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", $"{CCategory.CARR_JOB_START}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=416A,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.CARR_JOB_START}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4025", "Tray Exist");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.CARR_JOB_START}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4053", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.CARR_JOB_START}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4053", "Reading Type");

            #endregion

            #region 7.2.2.3 [G2-3] Carrier Output Report - Loc101
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CARR_OUT_RPT}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3024", "Loc101 Carrier(Tray) Out Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CARR_OUT_RPT}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3024", "Loc101 Carrier(Tray) Out Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CARR_OUT_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4024", "Loc101 Carrier(Tray) Out");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", $"{CCategory.CARR_OUT_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4160,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.CARR_OUT_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4020", "Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_LOT_ID", $"{CCategory.CARR_OUT_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=40F8,LENGTH=16", "LOTID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.CARR_OUT_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4108,LENGTH=10", "Group LOTID");
            __INTERNAL_VARIABLE_STRING("I_W_PRODUCT_ID", $"{CCategory.CARR_OUT_RPT}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4100,LENGTH=16", "PRODUCTID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.CARR_OUT_RPT}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4050", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.CARR_OUT_RPT}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4050", "Reading Type");

            #endregion

            #region 7.2.2.3 [G2-3] Carrier Output Report_02 - Loc202
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CARR_OUT_RPT}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3029", "Loc202 Carrier(Tray) Out Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CARR_OUT_RPT}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3029", "Loc202 Carrier(Tray) Out Ack");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID", $"{CCategory.CARR_OUT_RPT}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3780,LENGTH=16", "LOTID");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID", $"{CCategory.CARR_OUT_RPT}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3788,LENGTH=10", "Group LOTID");
            __INTERNAL_VARIABLE_STRING("O_W_ROUTE_ID", $"{CCategory.CARR_OUT_RPT}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3768,LENGTH=16", "ROUTE ID");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_INFO", $"{CCategory.CARR_OUT_RPT}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3770,LENGTH=1", "SPECIAL_INFO");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_NO", $"{CCategory.CARR_OUT_RPT}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3778,LENGTH=10", "SPECIAL_INFO_NO");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_TYPE", $"{CCategory.CARR_OUT_RPT}_02", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=377D,LENGTH=1", "Lot Type");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CARR_OUT_RPT}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4029", "Loc202 Carrier(Tray) Out");
            __INTERNAL_VARIABLE_STRING("I_W_JIG_ID", $"{CCategory.CARR_OUT_RPT}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4192,LENGTH=10", "Transfer Jig ID");

            #endregion

            #region 7.2.2.3 [G2-3] Carrier Output Report_03  - Loc104
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=302E", "Loc104 Carrier(Tray) Out Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=302E", "Loc104 Carrier(Tray) Out Ack");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=402E", "Loc104 Carrier(Tray) Out");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=416A,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4025", "Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4187,LENGTH=10", "Group LOTID");
            __INTERNAL_VARIABLE_STRING("I_W_ROUTE_ID", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4197,LENGTH=16", "ROUTE ID");
            __INTERNAL_VARIABLE_STRING("I_W_SPECIAL_INFO", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=418C,LENGTH=1", "SPECIAL_INFO");
            __INTERNAL_VARIABLE_STRING("I_W_SPECIAL_NO", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=418D,LENGTH=10", "SPECIAL_INFO_NO");
            __INTERNAL_VARIABLE_SHORT("I_W_CELL_CNT", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=419F", "CELL Count");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_EXIST_LIST", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=41A0,LENGTH=50", "CELL EXIST LIST");
            __INTERNAL_VARIABLE_STRINGLIST("I_W_CELL_ID_LIST", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, 40, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=41E0,LENGTH=20", "CELL ID LIST");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4053", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.CARR_OUT_RPT}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4053", "Reading Type");

            #endregion

            #region 7.2.2.3 [G2-3] Carrier Output Report_04 - Loc201
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CARR_OUT_RPT}_04", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=302F", "Loc201 Carrier(Tray) Out Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CARR_OUT_RPT}_04", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=302F", "Loc201 Carrier(Tray) Out Ack");
            __INTERNAL_VARIABLE_STRING("O_W_JIG_ID", $"{CCategory.CARR_OUT_RPT}_04", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3110,LENGTH=10", "Buffer Jig ID");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CARR_OUT_RPT}_04", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=402F", "Loc201 Carrier(Tray) Out");
            __INTERNAL_VARIABLE_SHORT("I_W_CELL_CNT", $"{CCategory.CARR_OUT_RPT}_04", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4740", "CELL Count");
            __INTERNAL_VARIABLE_STRING("I_W_ROUTE_ID", $"{CCategory.CARR_OUT_RPT}_04", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4741,LENGTH=16", "ROUTE ID");
            __INTERNAL_VARIABLE_STRING("I_W_GROUP_LOT_ID", $"{CCategory.CARR_OUT_RPT}_04", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4749,LENGTH=10", "LOTID");
            __INTERNAL_VARIABLE_STRINGLIST("I_W_CELL_ID_LIST", $"{CCategory.CARR_OUT_RPT}_04", enumAccessType.In, 40, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4760,LENGTH=20", "CELL ID LIST");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BUFFER_EMPTY_YN", $"{CCategory.CARR_OUT_RPT}_04", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4030", "Buffer Empty YN");
            #endregion

            #region 7.2.4.6 [G4-6] Cell Information Request - Loc201
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=304A", "Loc201 Cell Info Req Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=304A", "Loc201 Cell Info Req Ack");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID_1ST", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3568,LENGTH=16", "LOTID 1st");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID_1ST", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3570,LENGTH=16", "Group LOTID 1st");
            __INTERNAL_VARIABLE_STRING("O_W_ROUTE_ID_1ST", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3550,LENGTH=16", "ROUTE ID 1st");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_INFO_1ST", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3558,LENGTH=1", "SPECIAL_INFO 1st");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_NO_1ST", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3560,LENGTH=10", "SPECIAL_INFO_NO 1st");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID_2ND", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3590,LENGTH=16", "LOTID 2nd");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID_2ND", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3598,LENGTH=16", "Group LOTID 2nd");
            __INTERNAL_VARIABLE_STRING("O_W_ROUTE_ID_2ND", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3578,LENGTH=16", "ROUTE ID 2nd");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_INFO_2ND", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3580,LENGTH=1", "SPECIAL_INFO 2nd");
            __INTERNAL_VARIABLE_STRING("O_W_SPECIAL_NO_2ND", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=3588,LENGTH=10", "SPECIAL_INFO_NO 2nd");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=404A", "Loc201 Cell Info Req");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_1ST", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4550,LENGTH=20", "CELL ID 1st");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST_1ST", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=405A", "Cell Exist 1st");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_2ND", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4580,LENGTH=20", "CELL ID 2nd");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST_2ND", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=405B", "Cell Exist 2nd");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BUFFER_EMPTY_YN", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4030", "Buffer Empty YN");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4058", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.CELL_INFO_REQ}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4058", "Reading Type");

            #endregion

            #region 7.2.4.6 [G4-6] Cell Information Request_03 - Loc203
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=304B", "Loc203 Cell Info Req Conf");
            __INTERNAL_VARIABLE_SHORT(CTag.O_W_TRIGGER_REPORT_ACK, $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=304B", "Loc203 Cell Info Req Ack");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID_1ST", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35C8,LENGTH=10", "LOTID 1st");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID_1ST", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35D0,LENGTH=10", "Group LOTID 1st");
            __INTERNAL_VARIABLE_STRING("O_W_ROUTE_ID_1ST", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35B0,LENGTH=16", "ROUTE ID 1st");
            __INTERNAL_VARIABLE_STRING("O_W_LOT_ID_2ND", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35F0,LENGTH=10", "LOTID 2nd");
            __INTERNAL_VARIABLE_STRING("O_W_GROUP_LOT_ID_2ND", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35F8,LENGTH=10", "Group LOTID 2nd");
            __INTERNAL_VARIABLE_STRING("O_W_ROUTE_ID_2ND", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.Out, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=35D8,LENGTH=16", "ROUTE ID 2nd");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=404B", "Loc203 Cell Info Req");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_1ST", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=45B0,LENGTH=12", "CELL ID 1st");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST_1ST", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=405C", "Cell Exist 1st");
            __INTERNAL_VARIABLE_STRING("I_W_CELL_ID_2ND", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=45E0,LENGTH=12", "CELL ID 2nd");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_CELL_EXIST_2ND", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=405D", "Cell Exist 2nd");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_BCR_USING", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4059", "BCR Using");
            __INTERNAL_VARIABLE_SHORT("I_W_READING_TYPE", $"{CCategory.CELL_INFO_REQ}_03", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4059", "Reading Type");

            #endregion

            #region 7.3.7.5 [S7-5] Tray Transfer Info Request_01 - Port102
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3060", "Port102 Tray Transfer Info Req Conf");
            __INTERNAL_VARIABLE_SHORT("O_W_ACTION_CODE", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3060", "Transfer Action Code");
            __INTERNAL_VARIABLE_SHORT("O_W_STACK_COUNT", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3061", "Stack Count");
            __INTERNAL_VARIABLE_SHORT("O_W_WAIT_TIME", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3062", "WAIT TIME");
            __INTERNAL_VARIABLE_SHORT("O_W_BCR_READ_COUNT", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3063", "BCR Read Max Count");
            __INTERNAL_VARIABLE_BOOLEAN("O_B_BCR_RETRY", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3061", "BCR Read Retry");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4060", "Port102 Tray Transfer Info Req");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4065,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_STRING("I_W_STACKED_TRAY_ID", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4060,LENGTH=10", "Stacked Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4061", "Input Tray Exist");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_STACKED_TRAY_EXIST", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4062", "Stacked Tray Exist");
            __INTERNAL_VARIABLE_SHORT("I_W_WAIT_TIME", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=406B", "WAIT_TIME");
            __INTERNAL_VARIABLE_SHORT("I_W_BCR_READ_COUNT", $"{CCategory.TRAY_TRNSINFO}_01", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=406A", "BCR_READ_COUNT");

            #endregion

            #region 7.3.7.5 [S7-5] Tray Transfer Info Request_02 - Port104
            __INTERNAL_VARIABLE_BOOLEAN(CTag.O_B_TRIGGER_REPORT_CONF, $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3080", "Port104 Tray Transfer Info Req Conf");
            __INTERNAL_VARIABLE_SHORT("O_W_ACTION_CODE", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3080", "Transfer Action Code");
            __INTERNAL_VARIABLE_SHORT("O_W_STACK_COUNT", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3081", "Stack count");
            __INTERNAL_VARIABLE_SHORT("O_W_WAIT_TIME", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3082", "WAIT TIME");
            __INTERNAL_VARIABLE_SHORT("O_W_BCR_READ_COUNT", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.Out, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=3083", "BCR Read Max Count");
            __INTERNAL_VARIABLE_BOOLEAN("O_B_BCR_RETRY", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.Out, false, true, false, "DEVICE_TYPE=B,ADDRESS_NO=3081", "BCR Read Retry");

            __INTERNAL_VARIABLE_BOOLEAN(CTag.I_B_TRIGGER_REPORT, $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4080", "Port104 Tray Transfer Info Req");
            __INTERNAL_VARIABLE_STRING("I_W_TRAY_ID", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4085,LENGTH=10", "Tray ID");
            __INTERNAL_VARIABLE_STRING("I_W_STACKED_TRAY_ID", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4080,LENGTH=10", "Stacked Tray ID");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_TRAY_EXIST", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4081", "Input Tray Exist");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_STACKED_TRAY_EXIST", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4082", "Stacked Tray Exist");
            __INTERNAL_VARIABLE_SHORT("I_W_WAIT_TIME", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=408B", "WAIT_TIME");
            __INTERNAL_VARIABLE_SHORT("I_W_BCR_READ_COUNT", $"{CCategory.TRAY_TRNSINFO}_02", enumAccessType.In, 0, 0, false, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=408A", "BCR_READ_COUNT");

            #endregion

            #region 7.5.1.1 [T1-1] Port State Change Report_01 - Port101
            __INTERNAL_VARIABLE_SHORT("I_W_INPUT_ST_AVAIL", $"{CCategory.PORT_STAT_CHG}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A02", "Port101 Input Station Available");
            __INTERNAL_VARIABLE_SHORT("I_W_INPUT_ST_OP_MODE", $"{CCategory.PORT_STAT_CHG}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A00", "Input Station Operation Mode(1: Auto, 2: Manual, 3: Semi-Auto)");
            __INTERNAL_VARIABLE_SHORT("I_W_INPUT_ST_TF_TYPE", $"{CCategory.PORT_STAT_CHG}_01", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A01", "Input Station Transfer Type(1: AL)");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_INPUT_ST_TRAY_EXIST", $"{CCategory.PORT_STAT_CHG}_01", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4140", "Input Station Tray Exist");
            #endregion

            #region 7.5.1.1 [T1-1] Port State Change Report_02 - Port102
            __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_ST_AVAIL", $"{CCategory.PORT_STAT_CHG}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A32", "Port102 Output Station Available");
            __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_ST_OP_MODE", $"{CCategory.PORT_STAT_CHG}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A30", "Output Station Operation Mode(1: Auto, 2: Manual, 3: Semi-Auto)");
            __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_ST_TF_TYPE", $"{CCategory.PORT_STAT_CHG}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A31", "Output Station Transfer Type(1: AL)");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_OUTPUT_ST_TRAY_EXIST", $"{CCategory.PORT_STAT_CHG}_02", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4142", "Empty Output Station Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_OUTPUT_ST_TRAY_ID_LOWER", $"{CCategory.PORT_STAT_CHG}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4A36,LENGTH=10", "Output Station Lower Tray ID");
            __INTERNAL_VARIABLE_STRING("I_W_OUTPUT_ST_TRAY_ID_UPPER", $"{CCategory.PORT_STAT_CHG}_02", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4A3B,LENGTH=10", "Output Station Upper Tray ID");
            // 4A40, 4A48, 4A50, 4A58 추가 할지 말지?
            __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_ST_TRAY_TYPE", $"{CCategory.PORT_STAT_CHG}_02", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A33", "Output Station Transfer Type(1: Empty, 2:Actual)");

            #endregion

            #region 7.5.1.1 [T1-1] Port State Change Report_03 - Port103
            __INTERNAL_VARIABLE_SHORT("I_W_INPUT_ST_AVAIL", $"{CCategory.PORT_STAT_CHG}_03", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A62", "Port103 Input Station Available");
            __INTERNAL_VARIABLE_SHORT("I_W_INPUT_ST_OP_MODE", $"{CCategory.PORT_STAT_CHG}_03", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A60", "Input Station Operation Mode(1: Auto, 2: Manual, 3: Semi-Auto)");
            __INTERNAL_VARIABLE_SHORT("I_W_INPUT_ST_TF_TYPE", $"{CCategory.PORT_STAT_CHG}_03", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A61", "Input Station Transfer Type(1: AL)");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_INPUT_ST_TRAY_EXIST", $"{CCategory.PORT_STAT_CHG}_03", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4144", "Input Station Tray Exist");
            #endregion

            #region 7.5.1.1 [T1-1] Port State Change Report_04 - Port104
            __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_ST_AVAIL", $"{CCategory.PORT_STAT_CHG}_04", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A92", "Port104 Output Station Available");
            __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_ST_OP_MODE", $"{CCategory.PORT_STAT_CHG}_04", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A90", "Output Station Operation Mode(1: Auto, 2: Manual, 3: Semi-Auto)");
            __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_ST_TF_TYPE", $"{CCategory.PORT_STAT_CHG}_04", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4191", "Output Station Transfer Type(1: AL)");
            __INTERNAL_VARIABLE_BOOLEAN("I_B_OUTPUT_ST_TRAY_EXIST", $"{CCategory.PORT_STAT_CHG}_04", enumAccessType.In, true, true, false, "DEVICE_TYPE=B,ADDRESS_NO=4146", "Output Station Tray Exist");
            __INTERNAL_VARIABLE_STRING("I_W_OUTPUT_ST_TRAY_ID_LOWER", $"{CCategory.PORT_STAT_CHG}_04", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4A96,LENGTH=10", "Output Station Lower Tray ID");
            __INTERNAL_VARIABLE_STRING("I_W_OUTPUT_ST_TRAY_ID_UPPER", $"{CCategory.PORT_STAT_CHG}_04", enumAccessType.In, false, true, string.Empty, "DEVICE_TYPE=W,ADDRESS_NO=4A9B,LENGTH=10", "Output Station Upper Tray ID");
            // 4AA0, 4AA8, 4AB0, 4AB8 추가 할지 말지?
            __INTERNAL_VARIABLE_SHORT("I_W_OUTPUT_ST_TRAY_TYPE", $"{CCategory.PORT_STAT_CHG}_04", enumAccessType.In, 0, 0, true, true, 0, "DEVICE_TYPE=W,ADDRESS_NO=4A93", "Output Station Transfer Type(1: Empty, 2:Actual)");
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