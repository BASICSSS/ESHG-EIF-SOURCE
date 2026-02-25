using System;

namespace ESHG.EIF.FORM.EOLPACKER
{
    public static class TrigStatus
    {
        public const bool TRIG_ON = true;
        public const bool TRIG_OFF = false;
    }

    public static class ConfirmAck
    {
        public const ushort CLEAR = 00;
        public const ushort OK = 10;
        public const ushort NG = 11;
    }

    public static class OPSTATUS
    {
        public const string AUTO = "AUTO";
        public const string MANUAL = "MANUAL";
    }

    public static class OPMODE
    {
        public const string TEST = "TEST";
        public const string PROD = "PROD";
    }

    public static class LOTCTRLMODE
    {
        public const string REMOTE = "R";
        public const string LOCAL = "L";
    }

    public static class USERID
    {
        //public const string MMI = "MMI";
        public const string EIF = "EIF";
        public const string EQP = "EQP";  //2017.01.08 : Lagacy system에 사용하기 위하여 등록
    }

    public static class SHOPID
    {
        public const string EL = "EL";
        public const string ASSY = "ASSY";
        public const string FORM = "FORM";
    }

    public static class RESULT
    {
        public const string OK = "OK";
        public const string NG = "NG";
    }

    public static class ERRMSG
    {
        public const string ERRMSG_001 = "작업지시";

        public const string LOT_START_NG_WORKORDER = "No Workorder.";
        public const string LOT_START_NG_FIFO = "FIFO Violation.";
        public const string LOT_START_NG_LOTID = "NO LOT ID";
        public const string LOT_START_NG_PART_NO = "NO Part No.";

        public const string LOT_END_NG_LOTID_NULL = "Lot ID가 없습니다.";


        public const string EQP_STAT_ERR = "정의되지 않은 장비상태 입니다.";
        public const string IN_MTRL_VALID_ERR = "Input Material Validation Fail.";
        public const string IN_LOT_VALID_ERR = "Input LOT Validation Fail.";
        public const string GEN_LOT_FAIL = "LOT Generation Failed";

        public const string UBM_STAT_ERR = "사용할수 없는 UBM 입니다.";
    }

    public static class SHOPCODE_MLB
    {
        public const string OC1 = "OC1";
        public const string OC2 = "OC2";
        public const string OC3 = "OC3";
    }

    public static class SOCKETCODE
    {
        public const string STX = "\x02";
        public const string ETX = "\x03";
    }

    public static class EMS_STATUS
    {
        public const ushort OK = 10;
        public const ushort NG = 11;
    }

    public static class UBM_STATUS
    {
        public const ushort OK = 1;
        public const ushort NG_UBMID_CLEAR = 2;
        public const ushort NG_UBMID_NOT_CLEAR = 3;
    }

    //- Power Off = 0
    //- Run = 1
    //- Idle = 2
    //- Trouble = 4
    //- User Stop = 8
    //- 수동 BM = 16 (User Stop Sub State)
    //- 비조업 = 32 (User Stop Sub State)
    //- PM = 64 (User Stop Sub State)
    //- CM = 128 (User Stop Sub State)
    //- PD(by Operator) = 256 (User Stop Sub State)
    //- PD(by Host) = 257 (User Stop Sub State)
    //- UD = 512 (User Stop Sub State)
    //- 생산준비 = 1024 (User Stop Sub State)
    public static class EQPSTATUS
    {
        public const int OFF = 0;
        public const int RUN = 1;
        public const int WAIT = 2;
        public const int TROUBLE = 4;
        public const int USER_STOP = 8;
        public const int BM = 16;
        public const int NOT_WORK = 32;
        public const int PM = 64;
        public const int CM = 128;
        public const int PD_OP = 256;
        public const int PD_HOST = 257;
        public const int UD = 512;
        public const int STANDBY = 1024;
    }

    public static class EQPSTATUS2
    {
        public const string OFF = "0";
        public const string RUN = "1";
        public const string WAIT = "2";
        public const string TROUBLE = "4";
        public const string USERWAIT = "8";
    }

    public static class RMSSTATUS
    {
        public const int EQP_DISCONNECT = 0;
        public const int OFFLINE = 1;
        public const int ONLINE_LOCAL = 2;
        public const int ONLINE_REMOTE = 3;
        public const int ONLINE_1_5_V = 4;
        public const int AGENT_RESTART = 9;
    }

    public static class PLC_TYPE
    {
        //public const ushort MELSEC = 1 
        //public const string RUN = "1";
        //public const string WAIT = "2";
        //public const string TROUBLE = "4";
        //public const string USERWAIT = "8";
    }

    public static class SRCTYPE
    {
        public const string MES_UP = "UI";
        public const string EQUIPMENT = "EQ";
    }

    public static class IFMODE
    {
        public const string OnLine = "ON";
        public const string OffLine = "OFF";
        public const string TestMode = "TEST";
    }

    public static class ZZSMODE
    {
        public const string ON = "Z";
        public const string OFF = "N";
    }

    public static class MTRL_MNT_STAT
    {
        public const string ACTIVE = "A";
        public const string STANDBY = "S";
        public const string READY = "R";
    }

    /// <summary>
    /// 자재 Mount 유형
    /// </summary>
    public static class MTRL_OUTPUT_TYPE
    {
        /// <summary>
        /// Matierial 소진완료
        /// </summary>
        public const int MTRL_COMPLETE = 1;
        /// <summary>
        /// Matierial 잔량존재(재사용)
        /// </summary>
        public const int MTRL_REUSE_REMAIN = 2;
        /// <summary>
        /// Matierial 잔량존재(Holding)
        /// </summary>
        public const int MTRL_HOLD_REMAIN = 3;
        /// <summary>
        /// Validation NG
        /// </summary>
        public const int MTRL_VALID_NG = 4;
        /// <summary>
        /// 투입 전 Material 배출
        /// </summary>
        public const int MTRL_NOUSE_OUT = 5;
    }

    /// <summary>
    /// 자재 투입 유형
    /// </summary>
    public static class MTRL_MNT_TYPE
    {
        /// <summary>
        /// 잔량처리
        /// </summary>
        public const string REMAIN = "R";
        /// <summary>
        /// 계속사용
        /// </summary>
        public const string CONTINUE = "C";
        /// <summary>
        /// 투입완료
        /// </summary>
        public const string END = "E";
        /// <summary>
        /// 소진완료
        /// </summary>
        public const string COMPLETE = "C";
    }

    /// <summary>
    /// 잔량 자재 유형
    /// </summary>
    public static class MTRL_REMAIN_TYPE
    {
        /// <summary>
        /// Hold 배출
        /// </summary>
        public const string HOLD = "H";
        /// <summary>
        /// 정상배출
        /// </summary>
        public const string NORMAL = "N";
    }

    public static class MTRL_NOTCHING_MNT_POS
    {
        public const string LEFT = "PANCAKE_LEFT";
        public const string RIGHT = "PANCAKE_RIGHT";
        public const string OUT_LEFT = "REWINDER_#01";
        public const string OUT_RIGHT = "REWINDER_#02";
    }

    public static class MTRL_LAMI_MNT_POS
    {
        public const string CURRENT_PANCAKE_UPPER = "UPC_ID";
        public const string CURRENT_PANCAKE_MIDDLE = "MPC_ID";
        public const string CURRENT_PANCAKE_LOWER = "LPC_ID";
        public const string CURRENT_A_PANCAKE = "APC_ID";
        public const string CURRENT_C_PANCAKE = "CPC_ID";

        public const string NEXT_PANCAKE_UPPER = "NEXT_UPC_ID";
        public const string NEXT_PANCAKE_MIDDLE = "NEXT_MPC_ID";
        public const string NEXT_PANCAKE_LOWER = "NEXT_LPC_ID";

        public const string NEXT_A_PANCAKE = "NEXT_APC_ID";
        public const string NEXT_C_PANCAKE = "NEXT_CPC_ID";

        public const string CURRENT_SEPA_UPPER = "USEPARATOR_ID";
        public const string CURRENT_SEPA_LOWER = "LSEPARATOR_ID";

        public const string NEXT_SEPA_UPPER = "NEXT_USEPARATOR_ID";
        public const string NEXT_SEPA_LOWER = "NEXT_LSEPARATOR_ID";

        //2018-06-05 투입자재 TEST 용으로 추가
        public const string PANCAKE_UPPER_R = "RIGHT_UPC_ID";
        public const string PANCAKE_MIDDLE_R = "RIGHT_MPC_ID";
        public const string PANCAKE_LOWER_R = "RIGHT_LPC_ID";

        public const string PANCAKE_UPPER_L = "LEFT_UPC_ID";
        public const string PANCAKE_MIDDLE_L = "LEFT_MPC_ID";
        public const string PANCAKE_LOWER_L = "LEFT_LPC_ID";

        public const string SEPA_UPPER_R = "RIGHT_USEPARATOR_ID";
        public const string SEPA_LOWER_R = "RIGHT_LSEPARATOR_ID";

        public const string SEPA_UPPER_L = "LEFT_USEPARATOR_ID";
        public const string SEPA_LOWER_L = "LEFT_LSEPARATOR_ID";
        //2018-06-05 투입자재 TEST 용으로 추가


    }

    public static class MTRL_FD_MNT_POS
    {
        public const string ACTIVE_MAGAZINE_01 = "ACTIVE_PALLET_1";
        public const string ACTIVE_MAGAZINE_02 = "ACTIVE_PALLET_2";
        public const string ACTIVE_MAGAZINE_03 = "ACTIVE_PALLET_3";
        public const string ACTIVE_MAGAZINE_04 = "ACTIVE_PALLET_4";
        public const string ACTIVE_MAGAZINE_05 = "ACTIVE_PALLET_5";
        public const string ACTIVE_MAGAZINE_06 = "ACTIVE_PALLET_6";
        public const string ACTIVE_MAGAZINE_07 = "ACTIVE_PALLET_7";
        public const string ACTIVE_MAGAZINE_08 = "ACTIVE_PALLET_8";
        public const string ACTIVE_MAGAZINE_09 = "ACTIVE_PALLET_9";
        public const string ACTIVE_MAGAZINE_10 = "ACTIVE_PALLET_10";
        public const string ACTIVE_MAGAZINE_11 = "ACTIVE_PALLET_11";
        public const string ACTIVE_MAGAZINE_12 = "ACTIVE_PALLET_12";
        public const string ACTIVE_MAGAZINE_13 = "ACTIVE_PALLET_13";
        public const string ACTIVE_MAGAZINE_14 = "ACTIVE_PALLET_14";
        public const string ACTIVE_MAGAZINE_15 = "ACTIVE_PALLET_15";
        public const string ACTIVE_MAGAZINE_16 = "ACTIVE_PALLET_16";
        public const string ACTIVE_MAGAZINE_17 = "ACTIVE_PALLET_17";
        public const string ACTIVE_MAGAZINE_18 = "ACTIVE_PALLET_18";
        public const string ACTIVE_MAGAZINE_19 = "ACTIVE_PALLET_19";
        public const string ACTIVE_MAGAZINE_20 = "ACTIVE_PALLET_20";
        public const string NEXT_MAGAZINE_01 = "NEXT_PALLET_1";
        public const string NEXT_MAGAZINE_02 = "NEXT_PALLET_2";
        public const string NEXT_MAGAZINE_03 = "NEXT_PALLET_3";
        public const string NEXT_MAGAZINE_04 = "NEXT_PALLET_4";
        public const string NEXT_MAGAZINE_05 = "NEXT_PALLET_5";
        public const string NEXT_MAGAZINE_06 = "NEXT_PALLET_6";
        public const string NEXT_MAGAZINE_07 = "NEXT_PALLET_7";
        public const string NEXT_MAGAZINE_08 = "NEXT_PALLET_8";
        public const string NEXT_MAGAZINE_09 = "NEXT_PALLET_9";
        public const string NEXT_MAGAZINE_10 = "NEXT_PALLET_10";
        public const string NEXT_MAGAZINE_11 = "NEXT_PALLET_11";
        public const string NEXT_MAGAZINE_12 = "NEXT_PALLET_12";
        public const string NEXT_MAGAZINE_13 = "NEXT_PALLET_13";
        public const string NEXT_MAGAZINE_14 = "NEXT_PALLET_14";
        public const string NEXT_MAGAZINE_15 = "NEXT_PALLET_15";
        public const string NEXT_MAGAZINE_16 = "NEXT_PALLET_16";
        public const string NEXT_MAGAZINE_17 = "NEXT_PALLET_17";
        public const string NEXT_MAGAZINE_18 = "NEXT_PALLET_18";
        public const string NEXT_MAGAZINE_19 = "NEXT_PALLET_19";
        public const string NEXT_MAGAZINE_20 = "NEXT_PALLET_20";


        public const string CURRENT_SEPA = "SEPA_ID";
        public const string NEXT_SEPA = "NEXT_SEPA_ID";
    }

    public static class MTRL_PKG_MNT_POS
    {
        public const string BCR_01 = "BOX_BCR_1";
        public const string BCR_02 = "BOX_BCR_2";

        public const string BOX_01 = "LOADING_1";
        public const string BOX_02 = "LOADING_2";
        public const string BOX_03 = "LOADING_3";
        public const string BOX_04 = "LOADING_4";

        public const string FIRST_TAB_01 = "TAB_1_ACTIVE";
        public const string FIRST_TAB_02 = "TAB_1_STANDBY";
        public const string SECOND_TAB_01 = "TAB_2_ACTIVE";
        public const string SECOND_TAB_02 = "TAB_2_STANDBY";

        public const string FIRST_POUCH_01 = "POUCH_1_1";
        public const string FIRST_POUCH_02 = "POUCH_1_2";
        public const string SECOND_POUCH_01 = "POUCH_2_1";
        public const string SECOND_POUCH_02 = "POUCH_2_2";

        public const string EL_01 = "EL_1_ACTIVE";
        public const string EL_02 = "EL_2_ACTIVE";
        public const string EL_03 = "EL_3_ACTIVE";
        public const string EL_04 = "EL_4_ACTIVE";

        public const string CELL_01 = "CELL_1";
        public const string CELL_02 = "CELL_2";
        public const string CELL_03 = "CELL_3";
        public const string CELL_04 = "CELL_4";
        public const string CELL_05 = "CELL_5";
        public const string CELL_06 = "CELL_6";
        public const string CELL_07 = "CELL_7";
        public const string CELL_08 = "CELL_8";

        public const string IN_PRODUCT_ID = "IN_PRODUCT_ID";
        public const string IN_PRODUCT_ID1 = "IN_PRODUCT_ID1";
        public const string IN_PRODUCT_ID2 = "IN_PRODUCT_ID2";

        public const string FIRST_FILM_01 = "FILM_1_1";
        public const string FIRST_FILM_02 = "FILM_1_2";
        public const string SECOND_FILM_01 = "FILM_2_1";
        public const string SECOND_FILM_02 = "FILM_2_2";
    }

    public static class MTRL_STK_MNT_POS
    {
        public const string HALF_PALLET_ID = "HALF_PALLET_ID";
        public const string HALF_PALLET_ID_01 = "HALF_PALLET_ID_01";
        public const string HALF_PALLET_ID_02 = "HALF_PALLET_ID_02";
        public const string HALF_PALLET_ID_03 = "HALF_PALLET_ID_03";
        public const string HALF_PALLET_ID_04 = "HALF_PALLET_ID_04";

        public const string MONO_PALLET_ID = "MONO_PALLET_ID";
        public const string MONO_PALLET_ID_01 = "MONO_PALLET_ID_01";
        public const string MONO_PALLET_ID_02 = "MONO_PALLET_ID_02";
        public const string MONO_PALLET_ID_03 = "MONO_PALLET_ID_03";
        public const string MONO_PALLET_ID_04 = "MONO_PALLET_ID_04";
    }

    public static class NEXT_DAY
    {
        public const string YES = "Y";
        public const string NO = "N";
    }

    public static class COMMON_INTERVAL
    {
        public const int SEC_1 = 1000;
        public const int SEC_3 = 3000;
        public const int SEC_5 = 5000;
        public const int SEC_7 = 7000;
        public const int SEC_9 = 9000;
        public const int SEC_10 = 10000;
        public const int SEC_15 = 15000;
        public const int SEC_300 = 300000;
    }

    public static class COMMON_INTERVAL_SEC
    {
        public const int SEC_1 = 1;
        public const int SEC_2 = 2;
        public const int SEC_3 = 3;
        public const int SEC_4 = 4;
        public const int SEC_5 = 5;
        public const int SEC_6 = 6;
        public const int SEC_7 = 7;
        public const int SEC_8 = 8;
        public const int SEC_9 = 9;
        public const int SEC_10 = 10;
        public const int SEC_15 = 15;
        public const int SEC_20 = 20;
        public const int SEC_30 = 30;
    }

    public static class GLOBAL_LANGUAGE
    {
        public const int KOREA = 1;
        public const int ENGLISH = 2;
        public const int CHINA = 3;
        public const int POLAND = 4;
        public const int UKRAINE = 7;
        public const int RUSSIA = 8;
    }


    public static class GLOBAL_LANGUAGE_SET
    {
        public const string KOREA = "ko-KR";
        public const string ENGLISH = "en-US";
        public const string CHINA = "zh-CN";
        public const string POLAND = "pl-PL";
        public const string UKRAINE = "uk-UA";
        public const string RUSSIA = "ru-RU";
    }

    public static class CUT
    {
        public const string Yes = "Y";
        public const string No = "N";
    }

    public static class YESNO
    {
        public const string Yes = "Y";
        public const string No = "N";
    }

    public static class DEFECT_CODE
    {
        public const string NG01 = "NG1";
        public const string NG02 = "NG2";
        public const string NG03 = "NG3";
        public const string NG04 = "NG4";
        public const string NG05 = "NG5";
        public const string NG06 = "NG6";
        public const string NG07 = "NG7";
        public const string NG08 = "NG8";
        public const string NG09 = "NG9";
        public const string NG10 = "NG10";
        public const string NG11 = "NG11";
        public const string NG12 = "NG12";
        public const string NG13 = "NG13";
        public const string NG14 = "NG14";
        public const string NG15 = "NG15";
        public const string NG16 = "NG16";
        public const string NG17 = "NG17";
        public const string NG18 = "NG18";
        public const string NG19 = "NG19";
        public const string NG20 = "NG20";
        public const string NG21 = "NG21";
        public const string NG22 = "NG22";
        public const string NG23 = "NG23";
        public const string NG24 = "NG24";
        public const string NG25 = "NG25";
        public const string NG26 = "NG26";
        public const string NG27 = "NG27";
        public const string NG28 = "NG28";
        public const string NG29 = "NG29";
        public const string NG30 = "NG30";
    }

    public static class DEFECT_PKG_CODE
    {
        public const string FIRST_TAB_NG01 = "NG301";
        public const string FIRST_TAB_NG02 = "NG302";
        public const string FIRST_TAB_NG03 = "NG303";
        public const string FIRST_TAB_NG04 = "NG304";
        public const string FIRST_TAB_NG05 = "NG305";
        public const string FIRST_TAB_NG06 = "NG306";
        public const string FIRST_TAB_NG07 = "NG307";



        public const string SECOND_TAB_NG01 = "NG201";
        public const string SECOND_TAB_NG02 = "NG202";
        public const string SECOND_TAB_NG03 = "NG203";
        public const string SECOND_TAB_NG04 = "NG204";
        public const string SECOND_TAB_NG05 = "NG205";
        public const string SECOND_TAB_NG06 = "NG206";
        public const string SECOND_TAB_NG07 = "NG207";



        public const string ASSY_NG01 = "NG401";
        public const string ASSY_NG02 = "NG402";
        public const string ASSY_NG03 = "NG403";
        public const string ASSY_NG04 = "NG404";
        public const string ASSY_NG05 = "NG405";
        public const string ASSY_NG06 = "NG406";
        public const string ASSY_NG07 = "NG407";
        public const string ASSY_NG08 = "NG408";
        public const string ASSY_NG09 = "NG409";
        public const string ASSY_NG10 = "NG410";
        public const string ASSY_NG11 = "NG411";
        public const string ASSY_NG12 = "NG412";


        public const string EL_01_NG01 = "NG601";
        public const string EL_01_NG02 = "NG602";
        public const string EL_01_NG03 = "NG603";
        public const string EL_01_NG04 = "NG604";
        public const string EL_01_NG05 = "NG605";

        public const string EL_02_NG01 = "NG701";
        public const string EL_02_NG02 = "NG702";
        public const string EL_02_NG03 = "NG703";
        public const string EL_02_NG04 = "NG704";
        public const string EL_02_NG05 = "NG705";

        public const string WETTING_NG01 = "NG801";
        public const string WETTING_NG02 = "NG802";

        public const string VSEAL_01_NG01 = "NG901";
        public const string VSEAL_01_NG02 = "NG902";
        public const string VSEAL_01_NG03 = "NG903";
        public const string VSEAL_01_NG04 = "NG904";
        public const string VSEAL_01_NG05 = "NG905";

        public const string VSEAL_02_NG01 = "NG101";
        public const string VSEAL_02_NG02 = "NG102";
        public const string VSEAL_02_NG03 = "NG103";
        public const string VSEAL_02_NG04 = "NG104";
        public const string VSEAL_02_NG05 = "NG105";
        public const string VSEAL_02_NG06 = "NG106";
        public const string VSEAL_02_NG07 = "NG107";
    }

    //WAIT : 대기, ASSY_OUT : 조립 출고, FORM_IN : 활성화 입고
    public static class PKG_CARR_STAT
    {
        public const string CARR_WAIT = "WAIT";
        public const string CARR_ASSY_OUT = "ASSY_OUT";
        public const string CARR_FORM_IN = "FORM_IN";
    }

    public static class REMOTE_CMD
    {
        public const ushort CLEAR = 0;
        public const ushort RMS_ON_LINE = 1;
        public const ushort TEST_MODE_RELEASE = 11;
        public const ushort IT_BY_PASS_RELEASE = 12;
        public const ushort GRP_LOT_CTRL_MODE_CHG = 13;     // Remote
        public const ushort GRP_LOT_CTRL_MODE_LOCAL = 14;   // Local
        public const ushort PROCESSING_PAUSE = 21;
        public const ushort GRP_LOT_START = 31;
        public const ushort GRP_LOT_CHANGE = 32;
        public const ushort GRP_LOT_END = 33;
        public const ushort RFID_READING_MODE_ENABLE = 41;
    }

    public static class GRP_LOT_TYPE
    {
        public const ushort NORMAL = 1;
        public const ushort MODEL_CHG = 2;
        public const ushort CONDITION_ADJUST = 3;
        public const ushort REWORK = 4;
        public const ushort TEST = 5;
        public const ushort BOX_REWORK = 6;     // PKG : FOL/STK Rework
    }

    public static class GRP_LOT_TYPE_NAME
    {
        public const string NORMAL = "N";
        public const string MODEL_CHG = "C";
        public const string CONDITION_ADJUST = "A";
        public const string REWORK = "R";
        public const string BOX_REWORK = "F";   // PKG : FOL/STK Rework
        public const string TEST = "T";
    }

    public static class CELL_TYPE
    {
        public const ushort A_TYPE = 1;
        public const ushort C_TYPE = 2;
        public const ushort MONO_TYPE = 3;
        public const ushort HALF_TYPE = 4;
    }

    public static class CELL_TYPE_NAME
    {
        public const string A_TYPE = "A";
        public const string C_TYPE = "C";
        public const string MONO_TYPE = "M";
        public const string HALF_TYPE = "H";
    }

    public static class NND_WORK_TYPE
    {
        public const ushort NORMAL = 1;
        public const ushort VD_REWORK = 2;
        public const ushort REWINDING = 3;
    }

    public static class NND_WORK_TYPE_NAME
    {
        public const string NORMAL = "P";
        public const string VD_REWORK = "V";
        public const string REWINDING = "W";
    }

    public static class HOST_ALM_TYPE
    {
        public const ushort COMM_TYPE = 0;
        public const ushort YES_NO_TYPE = 1;
        public const ushort TYPE01 = 10;
        public const ushort TYPE02 = 20;
        public const ushort ALLTYPE = 90;
    }

    public static class TRS_MODE
    {
        public const string AGV = "AGV";
        public const string MGV = "MGV";
    }

    public static class PORT_TYPE
    {
        public const string LD = "LD";
        public const string UD = "UD";
    }

    public static class INOUT_TYPE
    {
        public const string IN = "IN";
        public const string OUT = "OUT";
    }

    public static class PORT_MODE
    {
        public const string AUTO = "AUTO";
        public const string MANUAL = "MANUAL";
    }

    public static class PORT_MODE_2
    {
        public const string AUTO = "A";
        public const string MANUAL = "M";
    }

    public static class PKG_PLC_NAME
    {
        public const string LOADER = "MELSEC_" + "LOADER";
        public const string FIRST_TAB_WELDER = "MELSEC_" + "FIRST_TAB_WELDER";
        public const string SECOND_TAB_WELDER = "MELSEC_" + "SECOND_TAB_WELDER";
        public const string CELL_ASSY = "MELSEC_" + "CELL_ASSY";
        public const string FORMING = "MELSEC_" + "FORMING";
        public const string EL_FILLING = "MELSEC_" + "EL_FILLING";
        public const string WETTING = "MELSEC_" + "WETTING";
        public const string VSEALING = "MELSEC_" + "VSEALING";
        public const string UNLOADER = "MELSEC_" + "UNLOADER";
    }

    public static class EQPT_LOT_PROG_MODE
    {
        public const string LampON = "ON";
        public const string LampOFF = "OFF";
        public const string NotUse = null;
    }

    /// <summary>
    /// Port State of Loader / Unloader
    /// </summary>
    public static class PORT_STATE_TYPE
    {
        public const ushort NONE = 0;           // None
        // Load Reqeust
        public const ushort LOAD_REQ = 1;
        // Loading
        public const ushort LOADING = 2;
        // Load Complete
        public const ushort LOAD_CMPLT = 3;
        // Unloading Reqeust
        public const ushort UNLOAD_REQ = 4;
        // Unloading
        public const ushort UNLOADING = 5;
        // Unloading Complete
        public const ushort UNLOAD_CMPLT = 6;
        // Port Lock
        public const ushort PORT_LOCK = 7;
        // Out of Service
        public const ushort OUT_SERVICE = 8;
    }

    public static class PORT_STATE
    {
        public const string LC = "LC";
        public const string LI = "LI";
        public const string LR = "LR";
        public const string LU = "LU";
        public const string LUI = "LUI";
        public const string NS = "NS";
        public const string PL = "PL";
        public const string UC = "UC";
        public const string UI = "UI";
        public const string UR = "UR";
    }

    public static class CELL_PSTN_TYPE
    {
        public const string PT = "PT"; // 셀 마킹 부  
        public const string EL = "EL"; // 주액배출부  
        public const string UL = "UL"; // Unloader 투입전        
    }

    public static class UBM_UNREPORT_KEY //상위로 보고하지 않는 UBM ID 
    {
        public const string CNB_VASSY = "12345"; //CNB 조립 설정값      
    }

    public static class PORT_ACCESS_MODE_CODE
    {
        public const string AUTO = "A";
        public const string MANUAL = "M";
    }

    public static class PORT_TRANSFER_CARRIER_STATE
    {
        public const string USING = "U";
        public const string EMPTY = "E";
        public const string EMPTYTRAY = "T";
    }


    #region CNB 2동 사용
    public static class CCategory
    {


        public const string EQP_COMM_CHK = "EQP_COMM_CHK";

        public const string HOST_COMM_CHK = "HOST_COMM_CHK";

        public const string COMM_STAT_CHG_RPT = "COMM_STAT_CHG_RPT";

        public const string DATE_TIME_SET_REQ = "DATE_TIME_SET_REQ";

        public const string EQP_STAT_CHG_RPT = "EQP_STAT_CHG_RPT";

        public const string ALARM_RPT = "ALARM_RPT";

        public const string EQP_OP_MODE_CHG_RPT = "EQP_OP_MODE_CHG_RPT";

        public const string PROCESS_STAT_CHG_RPT = "PROCESS_STAT_CHG_RPT";

        public const string REMOTE_COMM_SND = "REMOTE_COMM_SND";

        public const string WIP_DATA_RPT = "WIP_DATA_RPT";

        public const string HOST_ALARM_MSG_SEND = "HOST_ALARM_MSG_SEND";

        // JH 2024.07.23 Smoke detect 관련 추가
        public const string SMOKE_RPT = "SMOKE_RPT";

        public const string MTRL_MONITER_DATA = "G1_1_MTRL_MONITER_DATA";

        public const string MTRL_ID_REQ = "G1_2_MTRL_ID_REQ";

        public const string MTRL_STATE_CHG = "G1_3_MTRL_STATE_CHG";

        public const string MTRL_OUT_RPT = "G1_4_MTRL_OUT_RPT";

        public const string CARR_MONITER_DATA = "G2_0_CARR_MONITER_DATA";

        public const string CARR_ID_RPT = "G2_1_CARR_ID_RPT";

        public const string CARR_IN_RPT = "G2_2_CARR_IN_RPT";

        public const string CARR_JOB_START = "G2_2_CARR_JOB_START_RPT";

        public const string CARR_OUT_RPT = "G2_3_CARR_OUT_RPT";

        public const string CARR_STAT_CHG = "G2_4_CARR_STAT_CHG";

        public const string CARR_JOB_END = "G2_6_CARR_JOB_END";

        public const string LOT_INFO_REQ = "G3_1_LOT_INFO_REQ";

        public const string LOT_START_RPT = "G3_2_LOT_START_RPT";

        public const string LOT_END_RPT = "G3_3_LOT_END_RPT";

        public const string APD_RPT = "G3_5_APD_RPT";

        public const string DFT_DATA_RPT = "G3_7_DFT_DATA_RPT";

        public const string CELL_ID_CONF_REQ = "G4_1_CELL_ID_CONF_REQ";

        public const string CELL_IN_RPT = "G4_2_CELL_IN_RPT";

        public const string CELL_OUT_RPT = "G4_3_CELL_OUT_RPT";

        public const string CELL_STAT_CHG = "G4_4_CELL_STAT_CHG";

        public const string CELL_INFO_REQ = "G4_6_CELL_INFO_REQ";

        public const string CELL_CREATE_RPT = "G4_7_CELL_CREATE_RPT";


        public const string SECTION_DEF_DATA_RPT = "G5_3_SECTION_DEF_DATA_RPT";

        public const string DATUM_MARK_DETECT_RPT = "G5_12_DATUM_MARK_DETECT_RPT";


        public const string EQP_PART_IN_RPT = "G6_1_EQP_PART_IN_RPT";

        public const string EQP_PART_OUT_RPT = "G6_2_EQP_PART_OUT_RPT";

        public const string EQPT_PART_MONITOR = "G6_3_EQPT_PART_MONITOR";


        public const string PROC_PARA_CHG_RPT = "S5_1_PROC_PARA_CHG_RPT";

        public const string PROC_PARA_REQ = "S5_3_PROC_PARA_REQ";

        public const string SPOT_DEFECT_MARKING_RPT = "S5_5_SPOT_DEFECT_MARKING_RPT";


        public const string PORT_STAT_REFRESH_REQ = "T1_0_PORT_STAT_REFRESH_REQ";

        public const string PORT_STAT_CHG = "T1_1_PORT_STAT_CHG";

        public const string MACHINE_MON_DATA = "T1_2_MACHINE_MON_DATA";

        public const string PORT_TRANSPER_STATE_REQ = "T1_4_PORT_TRANSPER_STATE_REQ";

        public const string MTRL_IN_LINE_CONF_REQ = "T3_9_MTRL_IN_LINE_CONF_REQ";

        public const string MTRL_TRANSFER_STAT_REQ = "T3_10_MTRL_TRANSFER_STAT_REQ";

        public const string LINK_AREA = "Z1_LINK_AREA";

        public const string UTILITY_INFO = "B1_UTILITY_INFO";

        public const string MTRL_STAT_CHG = "G1_1_MTRL_STAT_CHG"; // JH 2024.11.14 재료 교체 알람 추가
    }
    #endregion

    public static class COMMON_CODE
    {
        public const string PORT_TRANSFER_CARRIER_STATE = "PORT_TRANSFER_CARRIER_STATE";
        public const string RFID_READING_TYPE = "RFID_READING_TYPE";
        public const string PORT_ACCESS_MODE_CODE = "PORT_ACCESS_MODE_CODE";

        public const string NND_WORK_TYPE = "NND_WORK_TYPE";
        public const string AN_LOT_TYPE = "AN_LOT_TYPE";
        public const string REMAIN_TYPE = "REMAIN_TYPE";
        public const string UNMOUNT_TYPE = "UNMOUNT_TYPE";
        public const string CELL_TYPE = "CELL_TYPE";
        public const string MTRL_OUT_TYPE = "MTRL_OUT_TYPE";
        public const string PKG_CELL_PSTN_ID = "PKG_CELL_PSTN_ID";
        public const string REWORK_TYPE = "REWORK_TYPE";

        public const string HALF_SLIT_SIDE = "HALF_SLIT_SIDE";
        public const string EM_SECTION_ROLL_DIRCTN = "EM_SECTION_ROLL_DIRCTN";

    }

    //$ 2023.05.18 : Alarm Set/Reset 설비 보고시 Type 정의(Alarm Set시 SET, Reset시 RST, 전체 해제시 : NULL)
    public static class ALMTYPE
    {
        public const string SET = "SET";                            // ALARM SET
        public const string RESET = "RST";                          // ALARM RESET
    }

    public struct DBTIME
    {
        public ushort wYear;
        public ushort wMonth;
        public ushort wDayOfWeek;
        public ushort wDay;
        public ushort wHour;
        public ushort wMinute;
        public ushort wSecond;
        public ushort wMilliseconds;
    }

    #region ----- Etc Class (CClctItem, CEioState, CBizRuleErr) --------------------------------------

    public class CClctItem
    {
        string EqpID = string.Empty;

        public string EQPID
        {
            get { return EqpID; }
            set { EqpID = value; }
        }

        int ClctItemNo = 0;

        public int CLCTITEMNO
        {
            get { return ClctItemNo; }
            set { ClctItemNo = value; }
        }

        string ClctType = string.Empty;

        public string CLCTTYPE
        {
            get { return ClctType; }
            set { ClctType = value; }
        }

        string ClctItem = string.Empty;

        public string CLCTITEM
        {
            get { return ClctItem; }
            set { ClctItem = value; }
        }

        int Fpoint = 0;

        public int FPOINT
        {
            get { return Fpoint; }
            set { Fpoint = value; }
        }

        int _Seq = 0;

        public CClctItem(int Seq)
        {
            _Seq = Seq;
        }
    }

    public class CDefectCode
    {
        string EqpID = string.Empty;

        public string EQPID
        {
            get { return EqpID; }
            set { EqpID = value; }
        }

        string EqpName = string.Empty;

        public string EQPNAME
        {
            get { return EqpName; }
            set { EqpName = value; }
        }

        int DefectCodeNo = 0;

        public int DEFECTCODENO
        {
            get { return DefectCodeNo; }
            set { DefectCodeNo = value; }
        }

        string DefectCode = string.Empty;

        public string DEFECTCODE
        {
            get { return DefectCode; }
            set { DefectCode = value; }
        }

        int _Seq = 0;

        public CDefectCode(int Seq)
        {
            _Seq = Seq;
        }
    }

    public class CEioState
    {
        string EqpState = string.Empty;

        public string EQPSTATE
        {
            get { return EqpState; }
            set { EqpState = value; }
        }

        string EioState = string.Empty;

        public string EIOSTATE
        {
            get { return EioState; }
            set { EioState = value; }
        }

        string ActId = string.Empty;

        public string ACTID
        {
            get { return ActId; }
            set { ActId = value; }
        }

        string EioNote = string.Empty;

        public string EIONOTE
        {
            get { return EioNote; }
            set { EioNote = value; }
        }

        int _Seq = 0;

        public CEioState(int Seq)
        {
            _Seq = Seq;
        }
    }
    #endregion

    #region BizRule 호출용 In/Out Class
    public class CInPallet
    {
        public string PALLETID { get; set; }
        public string BOXID { get; set; }
        //$ 2024.12.19 |tlsrmsdl1| :  PACKING_QTY Data Type 변경으로(Double -> Int32) 인해 수정진행
        public Int32 PACKING_QTY { get; set; }
        public string EMPTY_TRAY_FLAG { get; set; }
        public string OUT_PALLET_TYPE { get; set; }
        public string HOST_PALLETID { get; set; }
        public ushort TRAY_QTY { get; set; }
        public ushort TOTAL_CELL_QTY { get; set; }
        public string EMPTY_PALLET_FLAG { get; set; }
        public string INPUT_NEXT_PALLETID { get; set; }

        public CInPallet()
        {
        }
    }

    public class CInSubLot
    {
        public string SUBLOTID { get; set; }
        public string RESNCODE { get; set; }
        public string RESNDESC { get; set; }
        public string DFCT_CELL_FLAG { get; set; }
        public CInSubLot()
        {
        }
    }

    public class CInBox
    {
        //$ 2024.12.19 |tlsrmsdl1| :  PSTN_NO Data Type 변경으로(Double -> Int32) 인해 수정진행
        public Int32 PSTN_NO { get; set; }
        public string SUBLOTID { get; set; }

        public CInBox()
        {
        }
    }

    public class CInData
    {
        public string LOTID { get; set; }
        public string PORT_ID { get; set; }
        public string UPDUSER { get; set; }
        public string USERID { get; set; }
        public string EIOSTAT { get; set; }
        public string TRBL_CODE { get; set; }
        public string LOSS_CODE { get; set; }
        public string MCS_CST_ID { get; set; }
        public string CURR_INOUT_TYPE_CODE { get; set; }
        public string PORT_WRK_MODE { get; set; }
        public string INOUT_TYPE_CODE { get; set; }
        public string EQPTID { get; set; }
        public string PORT_STAT_CODE { get; set; }
        public string MTRL_EXIST_FLAG { get; set; }
        public string REQ_INOUT_TYPE_CODE { get; set; }
        public string EMPTY_CST_REQ_FLAG { get; set; }
        public double CST_QTY { get; set; }
        public string WORK_TYPE { get; set; }

        public CInData()
        {
        }
    }

    public class COutEqp
    {
        public string FORM_LINEID { get; set; }
        public double BOX_QTY { get; set; }
        public double SUBLOT_QTY { get; set; }

        public COutEqp()
        {
        }
    }

    //20191230 RFID 리딩율 모니터링 
    public class CInScan
    {
        public string IN_OUT_TYPE { get; set; }
        public string EQPT_MOUNT_PSTN_ID { get; set; }
        public string SCAN_TYPE { get; set; }
        public string SCAN_RSLT { get; set; }
        public string CSTID { get; set; }
        public string CST_LOAD_LAYER_CODE { get; set; }

        public CInScan()
        {
            IN_OUT_TYPE = "IN";
            SCAN_TYPE = "B";
            SCAN_RSLT = "NG";
            CSTID = "NO READ";
            CST_LOAD_LAYER_CODE = string.Empty;
        }
    }
    #endregion

    #region biz Class
    public class COutPallet
    {
        public string CSTID { get; set; }
        public string HOST_PALLETID { get; set; }
        public string OUT_PALLET_TYPE { get; set; }
        public string EQPT_END_FLAG { get; set; }

        public COutPallet()
        {
        }
    }
    #endregion

    #region Constant Class
    /// <summary>
    /// 배출 Pallet의 Output Type
    /// </summary>
    public static class PALLET_OUTPUT_TYPE
    {
        /// <summary>
        /// 공 Pallet
        /// </summary>
        public const int PALLET_EMPTY = 1;
        /// <summary>
        /// 공 Tray/Pallet
        /// </summary>
        public const int TRAY_PALLET_EMPTY = 2;
        /// <summary>
        /// 실 Pallet
        /// </summary>
        public const int PALLET_FULL = 3;
        /// <summary>
        /// 비정상 상태(강제배출)
        /// </summary>
        public const int PALLET_NG = 4;
    }

    /// <summary>
    /// Pallet 배출 유형
    /// </summary>
    public static class PALLET_STATUS
    {
        /// <summary>
        /// 공PLT
        /// </summary>
        public const string PLT_EMPTY = "E";
        /// <summary>
        /// 공TRAY PLT
        /// </summary>
        public const string TRAY_EMPTY = "T";
        /// <summary>
        /// 실PLT
        /// </summary>
        public const string PALLET_OK = "U";
        /// <summary>
        /// 강제배출
        /// </summary>
        public const string PALLET_NG = "F";
    }

    /// <summary>
    /// 배출 Tray의 Output Type
    /// </summary>
    public static class TRAY_OUTPUT_TYPE
    {
        /// <summary>
        /// 공 Tray
        /// </summary>
        public const int TRAY_EMPTY = 1;
        /// <summary>
        /// 실 Tray
        /// </summary>
        public const int TRAY_FULL = 2;
        /// <summary>
        /// 잔량 Tray
        /// </summary>
        public const int TRAY_REMAIN = 3;
    }

    /// <summary>
    /// Pallet 완공 보고 여부
    /// </summary>
    public static class EQPT_END_FLAG_CHK
    {
        /// <summary>
        /// 완공 보고 완료
        /// </summary>
        public const int COMPLETE = 1;
        /// <summary>
        /// 완공 보고 미완료
        /// </summary>
        public const int NOT_COMPLETE = 2;
    }
    #endregion

    //JH 2025.04.07 Host Alarm 표준화 관련 Class 생성
    #region HOST_MESSAGE_SEND CLASS
    public class HOSTMSG_SEND
    {
        public string TXN_ID { get; set; }
        public string inDTName { get; set; }
        public string outDTName { get; set; }
        public string actID { get; set; }
        public Refds refDS { get; set; }
    }

    public class Refds
    {
        public IN_DATA[] IN_DATA { get; set; }
    }

    public class IN_DATA
    {
        public string EQPTID { get; set; }
        public string SYS_NAME { get; set; }
        public int STOP_TYPE { get; set; }
        public int HMI_NO_1 { get; set; }
        public string MSGNAME_KOR_1 { get; set; }
        public string MSGNAME_ENG_1 { get; set; }
        public string MSGNAME_CHN_1 { get; set; }
        public string MSGNAME_POL_1 { get; set; }
        public string MSGNAME_ITA_1 { get; set; }
        public string MSGNAME_DEU_1 { get; set; }
        public string MSGNAME_UKR_1 { get; set; }
        public string MSGNAME_RUS_1 { get; set; }
        public string MSGNAME_IDN_1 { get; set; }
    }
    #endregion

    /// <summary>
    /// MCS CommunicationState 상태
    /// </summary>
    public enum CommunicationState
    {
        OFFLINE = 0,
        ONLINE = 1
    }

    public enum PLCConnectionState
    {
        OFFLINE = 0,
        ONLINE = 1
    }
}