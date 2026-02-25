using System.Linq;

namespace ESHG.EIF.FORM.EOLPACKER
{
    public enum REPORT_TYPE
    {
        PALLET_ID_RPT = 1,
        TRAY_ID_RPT,

        PALLET_JOB_START_RPT = 11,
        TRAY_JOB_START_RPT,
        PALLET_JOB_END_RPT,

        PALLET_OUT_RPT = 21,
        TRAY_END_PACKING,
        PALLET_CHECK_CONFIRM,

        APD_RPT = 31,

        CELL_ID_CONF_REQ = 41,
        CELL_OUT_NG,
        CELL_INFO_REQ,

        MODEL_ID_CHG = 51,
        LINE_ID_CHG,
        FIRST_TRAY_USE_CHG,

        PALET_INFO_REQ = 61,

        LR_PORT_STAT_CHG = 71,
        UR_PORT_STAT_CHG,

        MTRL_STAT_CHG_RPT = 92

    }

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


    public static class CTag
    {
        public const string I_B_TRIGGER_REPORT = "I_B_TRIGGER_REPORT";
        public const string O_B_TRIGGER_REPORT_CONF = "O_B_TRIGGER_REPORT_CONF";
        public const string O_W_TRIGGER_REPORT_ACK = "O_W_TRIGGER_REPORT_ACK";

        public const string I_W_TRIGGER_ = "I_W_TRIGGER_";
        public const string I_B_TRIGGER_ = "I_B_TRIGGER_";
        public const string O_B_TRIGGER_ = "O_B_TRIGGER_";

        public const string I_B_HOST_TRIGGER_CONF = "I_B_HOST_TRIGGER_CONF";
        public const string O_B_HOST_TRIGGER_REPORT = "O_B_HOST_TRIGGER_REPORT";

    }

    public class CReportInfo
    {
        public int _Type { get; set; }
        public string _Comment { get; set; }
        public char[] _listUseFlag { get; set; }
        public string _CategoryName { get; set; }
        public int _PstnNo { get; set; }

        public string _AddInfo01 { get; set; }
        public string _AddInfo02 { get; set; }


        public CReportInfo(int type, string CategoryName, string comment, string useFlag, int PstnNo = -1)
        {
            _Type = type;
            _Comment = comment;
            _listUseFlag = useFlag.ToArray();
            _CategoryName = CategoryName;
            _PstnNo = PstnNo;
        }

        public CReportInfo(int type, string CategoryName, string comment, string useFlag, string addInfo1, int PstnNo = -1)
        {
            _Type = type;
            _Comment = comment;
            _listUseFlag = useFlag.ToArray();
            _CategoryName = CategoryName;
            _PstnNo = PstnNo;
            _AddInfo01 = addInfo1;
        }
        public CReportInfo(int type, string CategoryName, string comment, string useFlag, string addInfo1, string addInfo2, int PstnNo = -1)
        {
            _Type = type;
            _Comment = comment;
            _listUseFlag = useFlag.ToArray();
            _CategoryName = CategoryName;
            _PstnNo = PstnNo;
            _AddInfo01 = addInfo1;
            _AddInfo02 = addInfo2;
        }

        public bool getUseFlag(int x)
        {
            if (x == -1 || _listUseFlag.Length < x) return true;

            if (_listUseFlag[x] == '0') return false;

            return true;
        }

        public bool getUseFlag(int x, bool _IsAccessFile)
        {
            if (_IsAccessFile)
            {
                bool chk = true;
                for (int i = 0; i < _listUseFlag.Length; i++)
                {
                    if (_listUseFlag[i] == '0') chk = false;
                }
                return chk;
            }
            else
            {
                if (x == -1 || _listUseFlag.Length <= x) return true;

                if (_listUseFlag[x] == '0') return false;

                return true;
            }
        }

        public int getReportType(string CategoryName)
        {
            if (_CategoryName.Equals(CategoryName)) return _Type;
            else return -1;
        }

    }
}