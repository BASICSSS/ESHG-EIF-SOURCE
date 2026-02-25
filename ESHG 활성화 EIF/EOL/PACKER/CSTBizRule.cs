/// <summary>
/// LGCNB.P01.EOL_MELSEC.CSTBizRule의 요약
/// </summary>
/// <filename>CSTBizRule.cs</filename>
/// <version>1.0.0.0</version>
/// <authors></authors>
/// <modifications>
/// 	1.	 ver1.0.0.0		2019.10.17 16:41:43		Developer Name	-
/// 		: Created.
/// </modifications>
/// <copyright>Copyright (c) 2007~. EzControl, LG CNS All rights reserved.</copyright>

using System.Collections.Generic;
using LGCNS.ezControl.Core;

namespace ESHG.EIF.FORM.EOL
{
    /// <summary>
    /// Enumeration of Host Adapter Type
    /// </summary>
    public enum HOST_ADAPTER_TYPE
    {
        /// <summary>
        /// 
        /// </summary>
        BizActor = 1,
        /// <summary>
        /// 
        /// </summary>
        BizActor_Java = 4,
        /// <summary>
        /// 
        /// </summary>
        BizActor_Remoting = 2,
        /// <summary>
        /// 
        /// </summary>
        BizActor_Remoting2 = 3,
        /// <summary>
        /// 
        /// </summary>
        Kafka = 5,
        /// <summary>
        /// 
        /// </summary>
        Simulation = 6,
    }

    #region CBR_GET_EOL_INPUT_CELL_CHECK_IN 의 요약
    /// <summary>
    /// TABLE_SEQ=INDATA
    /// </summary>
    public partial class CBR_GET_EOL_INPUT_CELL_CHECK_IN : CStructureVariable
    {
        /// <summary>
        /// Structure item from BizActor[2022-01-12 10:42:03]
        /// </summary>
        public CVariable __INDATA
        {
            get { return _struct["INDATA"]; }
        }

        #region CINDATA 의 요약
        /// <summary>
        /// Structure item from BizActor[2022-01-12 10:42:03]
        /// </summary>
        public partial class CINDATA : CStructureVariable
        {
            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:03]
            /// </summary>
            public CVariable __SRCTYPE
            {
                get { return _struct["SRCTYPE"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:03]
            /// </summary>
            public string SRCTYPE
            {
                get { return _struct["SRCTYPE"].AsString; }
                set { _struct["SRCTYPE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:03]
            /// </summary>
            public CVariable __IFMODE
            {
                get { return _struct["IFMODE"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:03]
            /// </summary>
            public string IFMODE
            {
                get { return _struct["IFMODE"].AsString; }
                set { _struct["IFMODE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:03]
            /// </summary>
            public CVariable __USERID
            {
                get { return _struct["USERID"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:03]
            /// </summary>
            public string USERID
            {
                get { return _struct["USERID"].AsString; }
                set { _struct["USERID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:03]
            /// </summary>
            public CVariable __EQPTID
            {
                get { return _struct["EQPTID"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:03]
            /// </summary>
            public string EQPTID
            {
                get { return _struct["EQPTID"].AsString; }
                set { _struct["EQPTID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:03]
            /// </summary>
            public CVariable __SUBLOTID
            {
                get { return _struct["SUBLOTID"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:03]
            /// </summary>
            public string SUBLOTID
            {
                get { return _struct["SUBLOTID"].AsString; }
                set { _struct["SUBLOTID"].AsString = value; }
            }

            public CINDATA(CStructureVariableCollection structure) : base(structure)
            {
            }

            public static CINDATA GetNew(CObject owner)
            {
                CVariable var = CStructureVariable.CreateStructureVariable(owner, "INDATA", "Structure item from BizActor[2022-01-12 10:42:03]");
                return var != null ? new CINDATA(var.Structure) : null;
            }
        }
        #endregion
        /// <summary>
        /// Structure item from BizActor[2022-01-12 10:42:03]
        /// </summary>
        public List<CINDATA> INDATA = new List<CINDATA>();
        /// <summary>
        /// Get, Set INDATA ListLength
        /// </summary>
        public int INDATA_LENGTH
        {
            get { return GetINDATALength(); }
            set { __INDATA.Length = value; }
        }
        private int GetINDATALength()
        {
            if (INDATA.Count != _struct["INDATA"].StructureList.Count)
            {
                UpdateINDATALength();
            }
            return INDATA.Count;
        }
        private void UpdateINDATALength()
        {
            INDATA.Clear();
            for (int i = 0; i < _struct["INDATA"].StructureList.Count; i++)
            {
                INDATA.Add(new CINDATA(_struct["INDATA"].StructureList[i]));
            }
        }
        public CBR_GET_EOL_INPUT_CELL_CHECK_IN(CStructureVariableCollection structure) : base(structure)
        {
            _struct["INDATA"].StructureList.OnListLengthUpdated += new LGCNS.ezControl.Common.delegateDo(UpdateINDATALength);
            UpdateINDATALength();

        }

        public static CBR_GET_EOL_INPUT_CELL_CHECK_IN GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureVariable(owner, "BR_GET_EOL_INPUT_CELL_CHECK_IN", "TABLE_SEQ=INDATA");
            return var != null ? new CBR_GET_EOL_INPUT_CELL_CHECK_IN(var.Structure) : null;
        }
    }
    #endregion
    #region CBR_GET_EOL_INPUT_CELL_CHECK_INList 의 요약
    /// <summary>
    /// TABLE_SEQ=INDATA (Structure List)
    /// </summary>
    public partial class CBR_GET_EOL_INPUT_CELL_CHECK_INList : List<CBR_GET_EOL_INPUT_CELL_CHECK_IN>
    {
        public CBR_GET_EOL_INPUT_CELL_CHECK_INList(List<CStructureVariableCollection> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Add(new CBR_GET_EOL_INPUT_CELL_CHECK_IN(list[i]));
            }

        }

        public static CBR_GET_EOL_INPUT_CELL_CHECK_INList GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureListVariable(owner, "BR_GET_EOL_INPUT_CELL_CHECK_IN");
            return var != null ? new CBR_GET_EOL_INPUT_CELL_CHECK_INList(var.StructureList) : null;
        }
    }
    #endregion
    #region CBR_GET_EOL_INPUT_CELL_CHECK_OUT 의 요약
    /// <summary>
    /// TABLE_SEQ=OUTDATA
    /// </summary>
    public partial class CBR_GET_EOL_INPUT_CELL_CHECK_OUT : CStructureVariable
    {
        /// <summary>
        /// Structure item from BizActor[2022-01-12 10:42:04]
        /// </summary>
        public CVariable __OUTDATA
        {
            get { return _struct["OUTDATA"]; }
        }

        #region COUTDATA 의 요약
        /// <summary>
        /// Structure item from BizActor[2022-01-12 10:42:04]
        /// </summary>
        public partial class COUTDATA : CStructureVariable
        {
            /// <summary>
            /// Long item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __RETVAL
            {
                get { return _struct["RETVAL"]; }
            }

            /// <summary>
            /// Long item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public long RETVAL
            {
                get { return _struct["RETVAL"].AsLong; }
                set { _struct["RETVAL"].AsLong = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __PASS_YN
            {
                get { return _struct["PASS_YN"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public string PASS_YN
            {
                get { return _struct["PASS_YN"].AsString; }
                set { _struct["PASS_YN"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __PROD_LOTID
            {
                get { return _struct["PROD_LOTID"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public string PROD_LOTID
            {
                get { return _struct["PROD_LOTID"].AsString; }
                set { _struct["PROD_LOTID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __SUBLOTJUDGE
            {
                get { return _struct["SUBLOTJUDGE"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public string SUBLOTJUDGE
            {
                get { return _struct["SUBLOTJUDGE"].AsString; }
                set { _struct["SUBLOTJUDGE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __MDLLOT_ID
            {
                get { return _struct["MDLLOT_ID"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public string MDLLOT_ID
            {
                get { return _struct["MDLLOT_ID"].AsString; }
                set { _struct["MDLLOT_ID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __TAB_CUTTING_INFO
            {
                get { return _struct["TAB_CUTTING_INFO"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public string TAB_CUTTING_INFO
            {
                get { return _struct["TAB_CUTTING_INFO"].AsString; }
                set { _struct["TAB_CUTTING_INFO"].AsString = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __CHECKITEM
            {
                get { return _struct["CHECKITEM"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int CHECKITEM
            {
                get { return _struct["CHECKITEM"].AsInteger; }
                set { _struct["CHECKITEM"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __VOLT_ULMT_VAL
            {
                get { return _struct["VOLT_ULMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int VOLT_ULMT_VAL
            {
                get { return _struct["VOLT_ULMT_VAL"].AsInteger; }
                set { _struct["VOLT_ULMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __VOLT_LLMT_VAL
            {
                get { return _struct["VOLT_LLMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int VOLT_LLMT_VAL
            {
                get { return _struct["VOLT_LLMT_VAL"].AsInteger; }
                set { _struct["VOLT_LLMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __ACIR_ULMT_VAL
            {
                get { return _struct["ACIR_ULMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int ACIR_ULMT_VAL
            {
                get { return _struct["ACIR_ULMT_VAL"].AsInteger; }
                set { _struct["ACIR_ULMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __ACIR_LLMT_VAL
            {
                get { return _struct["ACIR_LLMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int ACIR_LLMT_VAL
            {
                get { return _struct["ACIR_LLMT_VAL"].AsInteger; }
                set { _struct["ACIR_LLMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __IV_CA_ULMT_VAL
            {
                get { return _struct["IV_CA_ULMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int IV_CA_ULMT_VAL
            {
                get { return _struct["IV_CA_ULMT_VAL"].AsInteger; }
                set { _struct["IV_CA_ULMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __IV_CA_LLMT_VAL
            {
                get { return _struct["IV_CA_LLMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int IV_CA_LLMT_VAL
            {
                get { return _struct["IV_CA_LLMT_VAL"].AsInteger; }
                set { _struct["IV_CA_LLMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __WEIGHT_ULMT_VAL
            {
                get { return _struct["WEIGHT_ULMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int WEIGHT_ULMT_VAL
            {
                get { return _struct["WEIGHT_ULMT_VAL"].AsInteger; }
                set { _struct["WEIGHT_ULMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __WEIGHT_LLMT_VAL
            {
                get { return _struct["WEIGHT_LLMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int WEIGHT_LLMT_VAL
            {
                get { return _struct["WEIGHT_LLMT_VAL"].AsInteger; }
                set { _struct["WEIGHT_LLMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __THIC_ULMT_VAL
            {
                get { return _struct["THIC_ULMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int THIC_ULMT_VAL
            {
                get { return _struct["THIC_ULMT_VAL"].AsInteger; }
                set { _struct["THIC_ULMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __THIC_LLMT_VAL
            {
                get { return _struct["THIC_LLMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int THIC_LLMT_VAL
            {
                get { return _struct["THIC_LLMT_VAL"].AsInteger; }
                set { _struct["THIC_LLMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __PRESS_OCV_ULMT_VAL
            {
                get { return _struct["PRESS_OCV_ULMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int PRESS_OCV_ULMT_VAL
            {
                get { return _struct["PRESS_OCV_ULMT_VAL"].AsInteger; }
                set { _struct["PRESS_OCV_ULMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __PRESS_OCV_LLMT_VAL
            {
                get { return _struct["PRESS_OCV_LLMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int PRESS_OCV_LLMT_VAL
            {
                get { return _struct["PRESS_OCV_LLMT_VAL"].AsInteger; }
                set { _struct["PRESS_OCV_LLMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __PRODID
            {
                get { return _struct["PRODID"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public string PRODID
            {
                get { return _struct["PRODID"].AsString; }
                set { _struct["PRODID"].AsString = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __IR_LLMT_VAL
            {
                get { return _struct["IR_LLMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int IR_LLMT_VAL
            {
                get { return _struct["IR_LLMT_VAL"].AsInteger; }
                set { _struct["IR_LLMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __IR_ULMT_VAL
            {
                get { return _struct["IR_ULMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int IR_ULMT_VAL
            {
                get { return _struct["IR_ULMT_VAL"].AsInteger; }
                set { _struct["IR_ULMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __LOTID
            {
                get { return _struct["LOTID"]; }
            }

            /// <summary>
            /// String item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public string LOTID
            {
                get { return _struct["LOTID"].AsString; }
                set { _struct["LOTID"].AsString = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __COLDPRESS_IR_LLMT_VAL
            {
                get { return _struct["COLDPRESS_IR_LLMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int COLDPRESS_IR_LLMT_VAL
            {
                get { return _struct["COLDPRESS_IR_LLMT_VAL"].AsInteger; }
                set { _struct["COLDPRESS_IR_LLMT_VAL"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public CVariable __COLDPRESS_IR_ULMT_VAL
            {
                get { return _struct["COLDPRESS_IR_ULMT_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2022-01-12 10:42:04]
            /// </summary>
            public int COLDPRESS_IR_ULMT_VAL
            {
                get { return _struct["COLDPRESS_IR_ULMT_VAL"].AsInteger; }
                set { _struct["COLDPRESS_IR_ULMT_VAL"].AsInteger = value; }
            }

            public COUTDATA(CStructureVariableCollection structure) : base(structure)
            {
            }

            public static COUTDATA GetNew(CObject owner)
            {
                CVariable var = CStructureVariable.CreateStructureVariable(owner, "OUTDATA", "Structure item from BizActor[2022-01-12 10:42:04]");
                return var != null ? new COUTDATA(var.Structure) : null;
            }
        }
        #endregion
        /// <summary>
        /// Structure item from BizActor[2022-01-12 10:42:04]
        /// </summary>
        public List<COUTDATA> OUTDATA = new List<COUTDATA>();
        /// <summary>
        /// Get, Set OUTDATA ListLength
        /// </summary>
        public int OUTDATA_LENGTH
        {
            get { return GetOUTDATALength(); }
            set { __OUTDATA.Length = value; }
        }
        private int GetOUTDATALength()
        {
            if (OUTDATA.Count != _struct["OUTDATA"].StructureList.Count)
            {
                UpdateOUTDATALength();
            }
            return OUTDATA.Count;
        }
        private void UpdateOUTDATALength()
        {
            OUTDATA.Clear();
            for (int i = 0; i < _struct["OUTDATA"].StructureList.Count; i++)
            {
                OUTDATA.Add(new COUTDATA(_struct["OUTDATA"].StructureList[i]));
            }
        }
        public CBR_GET_EOL_INPUT_CELL_CHECK_OUT(CStructureVariableCollection structure) : base(structure)
        {
            _struct["OUTDATA"].StructureList.OnListLengthUpdated += new LGCNS.ezControl.Common.delegateDo(UpdateOUTDATALength);
            UpdateOUTDATALength();

        }

        public static CBR_GET_EOL_INPUT_CELL_CHECK_OUT GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureVariable(owner, "BR_GET_EOL_INPUT_CELL_CHECK_OUT", "TABLE_SEQ=OUTDATA");
            return var != null ? new CBR_GET_EOL_INPUT_CELL_CHECK_OUT(var.Structure) : null;
        }
    }
    #endregion
    #region CBR_GET_EOL_INPUT_CELL_CHECK_OUTList 의 요약
    /// <summary>
    /// TABLE_SEQ=OUTDATA (Structure List)
    /// </summary>
    public partial class CBR_GET_EOL_INPUT_CELL_CHECK_OUTList : List<CBR_GET_EOL_INPUT_CELL_CHECK_OUT>
    {
        public CBR_GET_EOL_INPUT_CELL_CHECK_OUTList(List<CStructureVariableCollection> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Add(new CBR_GET_EOL_INPUT_CELL_CHECK_OUT(list[i]));
            }

        }

        public static CBR_GET_EOL_INPUT_CELL_CHECK_OUTList GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureListVariable(owner, "BR_GET_EOL_INPUT_CELL_CHECK_OUT");
            return var != null ? new CBR_GET_EOL_INPUT_CELL_CHECK_OUTList(var.StructureList) : null;
        }
    }
    #endregion

    #region CBR_SET_EOL_JUDGED_CELL_IN 의 요약
    /// <summary>
    /// TABLE_SEQ=INDATA
    /// </summary>
    public partial class CBR_SET_EOL_JUDGED_CELL_IN : CStructureVariable
    {
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:34:18]
        /// </summary>
        public CVariable __INDATA
        {
            get { return _struct["INDATA"]; }
        }

        #region CINDATA 의 요약
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:34:18]
        /// </summary>
        public partial class CINDATA : CStructureVariable
        {
            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __EQPTID
            {
                get { return _struct["EQPTID"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public string EQPTID
            {
                get { return _struct["EQPTID"].AsString; }
                set { _struct["EQPTID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __SUBLOTID
            {
                get { return _struct["SUBLOTID"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public string SUBLOTID
            {
                get { return _struct["SUBLOTID"].AsString; }
                set { _struct["SUBLOTID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __USERID
            {
                get { return _struct["USERID"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public string USERID
            {
                get { return _struct["USERID"].AsString; }
                set { _struct["USERID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __BCR_NO
            {
                get { return _struct["BCR_NO"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public string BCR_NO
            {
                get { return _struct["BCR_NO"].AsString; }
                set { _struct["BCR_NO"].AsString = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __AVG_TCK_VALUE
            {
                get { return _struct["AVG_TCK_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int AVG_TCK_VALUE
            {
                get { return _struct["AVG_TCK_VALUE"].AsInteger; }
                set { _struct["AVG_TCK_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __TCK_MAX_VALUE
            {
                get { return _struct["TCK_MAX_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int TCK_MAX_VALUE
            {
                get { return _struct["TCK_MAX_VALUE"].AsInteger; }
                set { _struct["TCK_MAX_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __TCK_MIN_VALUE
            {
                get { return _struct["TCK_MIN_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int TCK_MIN_VALUE
            {
                get { return _struct["TCK_MIN_VALUE"].AsInteger; }
                set { _struct["TCK_MIN_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __VLTG_VALUE
            {
                get { return _struct["VLTG_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int VLTG_VALUE
            {
                get { return _struct["VLTG_VALUE"].AsInteger; }
                set { _struct["VLTG_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __ACIR_VALUE
            {
                get { return _struct["ACIR_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int ACIR_VALUE
            {
                get { return _struct["ACIR_VALUE"].AsInteger; }
                set { _struct["ACIR_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __WEIGHT_VALUE
            {
                get { return _struct["WEIGHT_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int WEIGHT_VALUE
            {
                get { return _struct["WEIGHT_VALUE"].AsInteger; }
                set { _struct["WEIGHT_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __PSTN1_TCK_VALUE
            {
                get { return _struct["PSTN1_TCK_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int PSTN1_TCK_VALUE
            {
                get { return _struct["PSTN1_TCK_VALUE"].AsInteger; }
                set { _struct["PSTN1_TCK_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __PSTN2_TCK_VALUE
            {
                get { return _struct["PSTN2_TCK_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int PSTN2_TCK_VALUE
            {
                get { return _struct["PSTN2_TCK_VALUE"].AsInteger; }
                set { _struct["PSTN2_TCK_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __PSTN3_TCK_VALUE
            {
                get { return _struct["PSTN3_TCK_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int PSTN3_TCK_VALUE
            {
                get { return _struct["PSTN3_TCK_VALUE"].AsInteger; }
                set { _struct["PSTN3_TCK_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __PSTN4_TCK_VALUE
            {
                get { return _struct["PSTN4_TCK_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int PSTN4_TCK_VALUE
            {
                get { return _struct["PSTN4_TCK_VALUE"].AsInteger; }
                set { _struct["PSTN4_TCK_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __PRESS_OCV_VALUE
            {
                get { return _struct["PRESS_OCV_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int PRESS_OCV_VALUE
            {
                get { return _struct["PRESS_OCV_VALUE"].AsInteger; }
                set { _struct["PRESS_OCV_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __TCK_MEASR_PSTN_NO
            {
                get { return _struct["TCK_MEASR_PSTN_NO"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int TCK_MEASR_PSTN_NO
            {
                get { return _struct["TCK_MEASR_PSTN_NO"].AsInteger; }
                set { _struct["TCK_MEASR_PSTN_NO"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __CA_IVLTG_VALUE
            {
                get { return _struct["CA_IVLTG_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int CA_IVLTG_VALUE
            {
                get { return _struct["CA_IVLTG_VALUE"].AsInteger; }
                set { _struct["CA_IVLTG_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __IR_VALUE
            {
                get { return _struct["IR_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int IR_VALUE
            {
                get { return _struct["IR_VALUE"].AsInteger; }
                set { _struct["IR_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __COLDPRESS_IR_VALUE
            {
                get { return _struct["COLDPRESS_IR_VALUE"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public int COLDPRESS_IR_VALUE
            {
                get { return _struct["COLDPRESS_IR_VALUE"].AsInteger; }
                set { _struct["COLDPRESS_IR_VALUE"].AsInteger = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __SRCTYPE
            {
                get { return _struct["SRCTYPE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public string SRCTYPE
            {
                get { return _struct["SRCTYPE"].AsString; }
                set { _struct["SRCTYPE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public CVariable __IFMODE
            {
                get { return _struct["IFMODE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:18]
            /// </summary>
            public string IFMODE
            {
                get { return _struct["IFMODE"].AsString; }
                set { _struct["IFMODE"].AsString = value; }
            }

            public CINDATA(CStructureVariableCollection structure) : base(structure)
            {
            }

            public static CINDATA GetNew(CObject owner)
            {
                CVariable var = CStructureVariable.CreateStructureVariable(owner, "INDATA", "Structure item from BizActor[2024-11-20 14:34:18]");
                return var != null ? new CINDATA(var.Structure) : null;
            }
        }
        #endregion
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:34:18]
        /// </summary>
        public List<CINDATA> INDATA = new List<CINDATA>();
        /// <summary>
        /// Get, Set INDATA ListLength
        /// </summary>
        public int INDATA_LENGTH
        {
            get { return GetINDATALength(); }
            set { __INDATA.Length = value; }
        }
        private int GetINDATALength()
        {
            if (INDATA.Count != _struct["INDATA"].StructureList.Count)
            {
                UpdateINDATALength();
            }
            return INDATA.Count;
        }
        private void UpdateINDATALength()
        {
            INDATA.Clear();
            for (int i = 0; i < _struct["INDATA"].StructureList.Count; i++)
            {
                INDATA.Add(new CINDATA(_struct["INDATA"].StructureList[i]));
            }
        }
        public CBR_SET_EOL_JUDGED_CELL_IN(CStructureVariableCollection structure) : base(structure)
        {
            _struct["INDATA"].StructureList.OnListLengthUpdated += new LGCNS.ezControl.Common.delegateDo(UpdateINDATALength);
            UpdateINDATALength();

        }

        public static CBR_SET_EOL_JUDGED_CELL_IN GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureVariable(owner, "BR_SET_EOL_JUDGED_CELL_IN", "TABLE_SEQ=INDATA");
            return var != null ? new CBR_SET_EOL_JUDGED_CELL_IN(var.Structure) : null;
        }
    }
    #endregion
    #region CBR_SET_EOL_JUDGED_CELL_INList 의 요약
    /// <summary>
    /// TABLE_SEQ=INDATA (Structure List)
    /// </summary>
    public partial class CBR_SET_EOL_JUDGED_CELL_INList : List<CBR_SET_EOL_JUDGED_CELL_IN>
    {
        public CBR_SET_EOL_JUDGED_CELL_INList(List<CStructureVariableCollection> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Add(new CBR_SET_EOL_JUDGED_CELL_IN(list[i]));
            }

        }

        public static CBR_SET_EOL_JUDGED_CELL_INList GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureListVariable(owner, "BR_SET_EOL_JUDGED_CELL_IN");
            return var != null ? new CBR_SET_EOL_JUDGED_CELL_INList(var.StructureList) : null;
        }
    }
    #endregion


    #region CBR_SET_EOL_JUDGED_CELL_OUT 의 요약
    /// <summary>
    /// TABLE_SEQ=OUTDATA
    /// </summary>
    public partial class CBR_SET_EOL_JUDGED_CELL_OUT : CStructureVariable
    {
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:34:47]
        /// </summary>
        public CVariable __OUTDATA
        {
            get { return _struct["OUTDATA"]; }
        }

        #region COUTDATA 의 요약
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:34:47]
        /// </summary>
        public partial class COUTDATA : CStructureVariable
        {
            /// <summary>
            /// Long item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __RETVAL
            {
                get { return _struct["RETVAL"]; }
            }

            /// <summary>
            /// Long item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public long RETVAL
            {
                get { return _struct["RETVAL"].AsLong; }
                set { _struct["RETVAL"].AsLong = value; }
            }

            /// <summary>
            /// Long item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __MVDAY_VALUE
            {
                get { return _struct["MVDAY_VALUE"]; }
            }

            /// <summary>
            /// Long item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public long MVDAY_VALUE
            {
                get { return _struct["MVDAY_VALUE"].AsLong; }
                set { _struct["MVDAY_VALUE"].AsLong = value; }
            }

            /// <summary>
            /// Long item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __MVDAY_SPEC_VALUE
            {
                get { return _struct["MVDAY_SPEC_VALUE"]; }
            }

            /// <summary>
            /// Long item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public long MVDAY_SPEC_VALUE
            {
                get { return _struct["MVDAY_SPEC_VALUE"].AsLong; }
                set { _struct["MVDAY_SPEC_VALUE"].AsLong = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __MVDAY_JUDG_RSLT_CODE
            {
                get { return _struct["MVDAY_JUDG_RSLT_CODE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public string MVDAY_JUDG_RSLT_CODE
            {
                get { return _struct["MVDAY_JUDG_RSLT_CODE"].AsString; }
                set { _struct["MVDAY_JUDG_RSLT_CODE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __MVDAY_JUDG
            {
                get { return _struct["MVDAY_JUDG"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public string MVDAY_JUDG
            {
                get { return _struct["MVDAY_JUDG"].AsString; }
                set { _struct["MVDAY_JUDG"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __SUBLOTJUDGE
            {
                get { return _struct["SUBLOTJUDGE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public string SUBLOTJUDGE
            {
                get { return _struct["SUBLOTJUDGE"].AsString; }
                set { _struct["SUBLOTJUDGE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __SUBLOTJUDGE_PASS_YN
            {
                get { return _struct["SUBLOTJUDGE_PASS_YN"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public string SUBLOTJUDGE_PASS_YN
            {
                get { return _struct["SUBLOTJUDGE_PASS_YN"].AsString; }
                set { _struct["SUBLOTJUDGE_PASS_YN"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __JUDG_PASS_FLAG
            {
                get { return _struct["JUDG_PASS_FLAG"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public string JUDG_PASS_FLAG
            {
                get { return _struct["JUDG_PASS_FLAG"].AsString; }
                set { _struct["JUDG_PASS_FLAG"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __PRINT_2D_DATA
            {
                get { return _struct["PRINT_2D_DATA"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public string PRINT_2D_DATA
            {
                get { return _struct["PRINT_2D_DATA"].AsString; }
                set { _struct["PRINT_2D_DATA"].AsString = value; }
            }

            /// <summary>
            /// Int16 item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __PRINT_2D_LEN_VALUE
            {
                get { return _struct["PRINT_2D_LEN_VALUE"]; }
            }

            /// <summary>
            /// Int16 item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public ushort PRINT_2D_LEN_VALUE
            {
                get { return _struct["PRINT_2D_LEN_VALUE"].AsShort; }
                set { _struct["PRINT_2D_LEN_VALUE"].AsShort = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __PRINT_GBT_DATA
            {
                get { return _struct["PRINT_GBT_DATA"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public string PRINT_GBT_DATA
            {
                get { return _struct["PRINT_GBT_DATA"].AsString; }
                set { _struct["PRINT_GBT_DATA"].AsString = value; }
            }

            /// <summary>
            /// Int16 item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __PRINT_GBT_LEN_VALUE
            {
                get { return _struct["PRINT_GBT_LEN_VALUE"]; }
            }

            /// <summary>
            /// Int16 item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public ushort PRINT_GBT_LEN_VALUE
            {
                get { return _struct["PRINT_GBT_LEN_VALUE"].AsShort; }
                set { _struct["PRINT_GBT_LEN_VALUE"].AsShort = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __PRINT_MODE_CODE
            {
                get { return _struct["PRINT_MODE_CODE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public string PRINT_MODE_CODE
            {
                get { return _struct["PRINT_MODE_CODE"].AsString; }
                set { _struct["PRINT_MODE_CODE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __EOL_JUDG_RSLT_CODE
            {
                get { return _struct["EOL_JUDG_RSLT_CODE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public string EOL_JUDG_RSLT_CODE
            {
                get { return _struct["EOL_JUDG_RSLT_CODE"].AsString; }
                set { _struct["EOL_JUDG_RSLT_CODE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __EOL_DFCT_CLSS_CODE
            {
                get { return _struct["EOL_DFCT_CLSS_CODE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public string EOL_DFCT_CLSS_CODE
            {
                get { return _struct["EOL_DFCT_CLSS_CODE"].AsString; }
                set { _struct["EOL_DFCT_CLSS_CODE"].AsString = value; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public CVariable __COMPUTE_VAL
            {
                get { return _struct["COMPUTE_VAL"]; }
            }

            /// <summary>
            /// Int32 item from BizActor[2024-11-20 14:34:47]
            /// </summary>
            public int COMPUTE_VAL
            {
                get { return _struct["COMPUTE_VAL"].AsInteger; }
                set { _struct["COMPUTE_VAL"].AsInteger = value; }
            }

            public COUTDATA(CStructureVariableCollection structure) : base(structure)
            {
            }

            public static COUTDATA GetNew(CObject owner)
            {
                CVariable var = CStructureVariable.CreateStructureVariable(owner, "OUTDATA", "Structure item from BizActor[2024-11-20 14:34:47]");
                return var != null ? new COUTDATA(var.Structure) : null;
            }
        }
        #endregion
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:34:47]
        /// </summary>
        public List<COUTDATA> OUTDATA = new List<COUTDATA>();
        /// <summary>
        /// Get, Set OUTDATA ListLength
        /// </summary>
        public int OUTDATA_LENGTH
        {
            get { return GetOUTDATALength(); }
            set { __OUTDATA.Length = value; }
        }
        private int GetOUTDATALength()
        {
            if (OUTDATA.Count != _struct["OUTDATA"].StructureList.Count)
            {
                UpdateOUTDATALength();
            }
            return OUTDATA.Count;
        }
        private void UpdateOUTDATALength()
        {
            OUTDATA.Clear();
            for (int i = 0; i < _struct["OUTDATA"].StructureList.Count; i++)
            {
                OUTDATA.Add(new COUTDATA(_struct["OUTDATA"].StructureList[i]));
            }
        }
        public CBR_SET_EOL_JUDGED_CELL_OUT(CStructureVariableCollection structure) : base(structure)
        {
            _struct["OUTDATA"].StructureList.OnListLengthUpdated += new LGCNS.ezControl.Common.delegateDo(UpdateOUTDATALength);
            UpdateOUTDATALength();

        }

        public static CBR_SET_EOL_JUDGED_CELL_OUT GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureVariable(owner, "BR_SET_EOL_JUDGED_CELL_OUT", "TABLE_SEQ=OUTDATA");
            return var != null ? new CBR_SET_EOL_JUDGED_CELL_OUT(var.Structure) : null;
        }
    }
    #endregion
    #region CBR_SET_EOL_JUDGED_CELL_OUTList 의 요약
    /// <summary>
    /// TABLE_SEQ=OUTDATA (Structure List)
    /// </summary>
    public partial class CBR_SET_EOL_JUDGED_CELL_OUTList : List<CBR_SET_EOL_JUDGED_CELL_OUT>
    {
        public CBR_SET_EOL_JUDGED_CELL_OUTList(List<CStructureVariableCollection> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Add(new CBR_SET_EOL_JUDGED_CELL_OUT(list[i]));
            }

        }

        public static CBR_SET_EOL_JUDGED_CELL_OUTList GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureListVariable(owner, "BR_SET_EOL_JUDGED_CELL_OUT");
            return var != null ? new CBR_SET_EOL_JUDGED_CELL_OUTList(var.StructureList) : null;
        }
    }
    #endregion

    #region CBR_SET_EOL_2D_PRINT_VERIFICATION_IN 의 요약
    /// <summary>
    /// TABLE_SEQ=INDATA
    /// </summary>
    public partial class CBR_SET_EOL_2D_PRINT_VERIFICATION_IN : CStructureVariable
    {
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:36:41]
        /// </summary>
        public CVariable __INDATA
        {
            get { return _struct["INDATA"]; }
        }

        #region CINDATA 의 요약
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:36:41]
        /// </summary>
        public partial class CINDATA : CStructureVariable
        {
            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __SRCTYPE
            {
                get { return _struct["SRCTYPE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public string SRCTYPE
            {
                get { return _struct["SRCTYPE"].AsString; }
                set { _struct["SRCTYPE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __IFMODE
            {
                get { return _struct["IFMODE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public string IFMODE
            {
                get { return _struct["IFMODE"].AsString; }
                set { _struct["IFMODE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __EQPTID
            {
                get { return _struct["EQPTID"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public string EQPTID
            {
                get { return _struct["EQPTID"].AsString; }
                set { _struct["EQPTID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __USERID
            {
                get { return _struct["USERID"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public string USERID
            {
                get { return _struct["USERID"].AsString; }
                set { _struct["USERID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __SUBLOTID
            {
                get { return _struct["SUBLOTID"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public string SUBLOTID
            {
                get { return _struct["SUBLOTID"].AsString; }
                set { _struct["SUBLOTID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __PRINT_GBT_DATA
            {
                get { return _struct["PRINT_GBT_DATA"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public string PRINT_GBT_DATA
            {
                get { return _struct["PRINT_GBT_DATA"].AsString; }
                set { _struct["PRINT_GBT_DATA"].AsString = value; }
            }

            /// <summary>
            /// Int16 item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __PRINT_GBT_LEN_VALUE
            {
                get { return _struct["PRINT_GBT_LEN_VALUE"]; }
            }

            /// <summary>
            /// Int16 item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public ushort PRINT_GBT_LEN_VALUE
            {
                get { return _struct["PRINT_GBT_LEN_VALUE"].AsShort; }
                set { _struct["PRINT_GBT_LEN_VALUE"].AsShort = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __PRINT_GBT_VERIFY_GRADE
            {
                get { return _struct["PRINT_GBT_VERIFY_GRADE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public string PRINT_GBT_VERIFY_GRADE
            {
                get { return _struct["PRINT_GBT_VERIFY_GRADE"].AsString; }
                set { _struct["PRINT_GBT_VERIFY_GRADE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __PRINT_2D_DATA
            {
                get { return _struct["PRINT_2D_DATA"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public string PRINT_2D_DATA
            {
                get { return _struct["PRINT_2D_DATA"].AsString; }
                set { _struct["PRINT_2D_DATA"].AsString = value; }
            }

            /// <summary>
            /// Int16 item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __PRINT_2D_LEN_VALUE
            {
                get { return _struct["PRINT_2D_LEN_VALUE"]; }
            }

            /// <summary>
            /// Int16 item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public ushort PRINT_2D_LEN_VALUE
            {
                get { return _struct["PRINT_2D_LEN_VALUE"].AsShort; }
                set { _struct["PRINT_2D_LEN_VALUE"].AsShort = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public CVariable __PRINT_2D_VERIFY_GRADE
            {
                get { return _struct["PRINT_2D_VERIFY_GRADE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:36:41]
            /// </summary>
            public string PRINT_2D_VERIFY_GRADE
            {
                get { return _struct["PRINT_2D_VERIFY_GRADE"].AsString; }
                set { _struct["PRINT_2D_VERIFY_GRADE"].AsString = value; }
            }

            public CINDATA(CStructureVariableCollection structure) : base(structure)
            {
            }

            public static CINDATA GetNew(CObject owner)
            {
                CVariable var = CStructureVariable.CreateStructureVariable(owner, "INDATA", "Structure item from BizActor[2024-11-20 14:36:41]");
                return var != null ? new CINDATA(var.Structure) : null;
            }
        }
        #endregion
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:36:41]
        /// </summary>
        public List<CINDATA> INDATA = new List<CINDATA>();
        /// <summary>
        /// Get, Set INDATA ListLength
        /// </summary>
        public int INDATA_LENGTH
        {
            get { return GetINDATALength(); }
            set { __INDATA.Length = value; }
        }
        private int GetINDATALength()
        {
            if (INDATA.Count != _struct["INDATA"].StructureList.Count)
            {
                UpdateINDATALength();
            }
            return INDATA.Count;
        }
        private void UpdateINDATALength()
        {
            INDATA.Clear();
            for (int i = 0; i < _struct["INDATA"].StructureList.Count; i++)
            {
                INDATA.Add(new CINDATA(_struct["INDATA"].StructureList[i]));
            }
        }
        public CBR_SET_EOL_2D_PRINT_VERIFICATION_IN(CStructureVariableCollection structure) : base(structure)
        {
            _struct["INDATA"].StructureList.OnListLengthUpdated += new LGCNS.ezControl.Common.delegateDo(UpdateINDATALength);
            UpdateINDATALength();

        }

        public static CBR_SET_EOL_2D_PRINT_VERIFICATION_IN GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureVariable(owner, "BR_SET_EOL_2D_PRINT_VERIFICATION_IN", "TABLE_SEQ=INDATA");
            return var != null ? new CBR_SET_EOL_2D_PRINT_VERIFICATION_IN(var.Structure) : null;
        }
    }
    #endregion
    #region CBR_SET_EOL_2D_PRINT_VERIFICATION_INList 의 요약
    /// <summary>
    /// TABLE_SEQ=INDATA (Structure List)
    /// </summary>
    public partial class CBR_SET_EOL_2D_PRINT_VERIFICATION_INList : List<CBR_SET_EOL_2D_PRINT_VERIFICATION_IN>
    {
        public CBR_SET_EOL_2D_PRINT_VERIFICATION_INList(List<CStructureVariableCollection> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Add(new CBR_SET_EOL_2D_PRINT_VERIFICATION_IN(list[i]));
            }

        }

        public static CBR_SET_EOL_2D_PRINT_VERIFICATION_INList GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureListVariable(owner, "BR_SET_EOL_2D_PRINT_VERIFICATION_IN");
            return var != null ? new CBR_SET_EOL_2D_PRINT_VERIFICATION_INList(var.StructureList) : null;
        }
    }
    #endregion
    #region CBR_SET_EOL_2D_PRINT_VERIFICATION_OUT 의 요약
    /// <summary>
    /// TABLE_SEQ=OUTDATA
    /// </summary>
    public partial class CBR_SET_EOL_2D_PRINT_VERIFICATION_OUT : CStructureVariable
    {
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:37:12]
        /// </summary>
        public CVariable __OUTDATA
        {
            get { return _struct["OUTDATA"]; }
        }

        #region COUTDATA 의 요약
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:37:12]
        /// </summary>
        public partial class COUTDATA : CStructureVariable
        {
            /// <summary>
            /// Long item from BizActor[2024-11-20 14:37:12]
            /// </summary>
            public CVariable __RETVAL
            {
                get { return _struct["RETVAL"]; }
            }

            /// <summary>
            /// Long item from BizActor[2024-11-20 14:37:12]
            /// </summary>
            public long RETVAL
            {
                get { return _struct["RETVAL"].AsLong; }
                set { _struct["RETVAL"].AsLong = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:37:12]
            /// </summary>
            public CVariable __PRINT_GBT_VERIFY_GRADE
            {
                get { return _struct["PRINT_GBT_VERIFY_GRADE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:37:12]
            /// </summary>
            public string PRINT_GBT_VERIFY_GRADE
            {
                get { return _struct["PRINT_GBT_VERIFY_GRADE"].AsString; }
                set { _struct["PRINT_GBT_VERIFY_GRADE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:37:12]
            /// </summary>
            public CVariable __PRINT_2D_VERIFY_GRADE
            {
                get { return _struct["PRINT_2D_VERIFY_GRADE"]; }
            }

            /// <summary>
            /// String item from BizActor[2024-11-20 14:37:12]
            /// </summary>
            public string PRINT_2D_VERIFY_GRADE
            {
                get { return _struct["PRINT_2D_VERIFY_GRADE"].AsString; }
                set { _struct["PRINT_2D_VERIFY_GRADE"].AsString = value; }
            }

            public COUTDATA(CStructureVariableCollection structure) : base(structure)
            {
            }

            public static COUTDATA GetNew(CObject owner)
            {
                CVariable var = CStructureVariable.CreateStructureVariable(owner, "OUTDATA", "Structure item from BizActor[2024-11-20 14:37:12]");
                return var != null ? new COUTDATA(var.Structure) : null;
            }
        }
        #endregion
        /// <summary>
        /// Structure item from BizActor[2024-11-20 14:37:12]
        /// </summary>
        public List<COUTDATA> OUTDATA = new List<COUTDATA>();
        /// <summary>
        /// Get, Set OUTDATA ListLength
        /// </summary>
        public int OUTDATA_LENGTH
        {
            get { return GetOUTDATALength(); }
            set { __OUTDATA.Length = value; }
        }
        private int GetOUTDATALength()
        {
            if (OUTDATA.Count != _struct["OUTDATA"].StructureList.Count)
            {
                UpdateOUTDATALength();
            }
            return OUTDATA.Count;
        }
        private void UpdateOUTDATALength()
        {
            OUTDATA.Clear();
            for (int i = 0; i < _struct["OUTDATA"].StructureList.Count; i++)
            {
                OUTDATA.Add(new COUTDATA(_struct["OUTDATA"].StructureList[i]));
            }
        }
        public CBR_SET_EOL_2D_PRINT_VERIFICATION_OUT(CStructureVariableCollection structure) : base(structure)
        {
            _struct["OUTDATA"].StructureList.OnListLengthUpdated += new LGCNS.ezControl.Common.delegateDo(UpdateOUTDATALength);
            UpdateOUTDATALength();

        }

        public static CBR_SET_EOL_2D_PRINT_VERIFICATION_OUT GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureVariable(owner, "BR_SET_EOL_2D_PRINT_VERIFICATION_OUT", "TABLE_SEQ=OUTDATA");
            return var != null ? new CBR_SET_EOL_2D_PRINT_VERIFICATION_OUT(var.Structure) : null;
        }
    }
    #endregion
    #region CBR_SET_EOL_2D_PRINT_VERIFICATION_OUTList 의 요약
    /// <summary>
    /// TABLE_SEQ=OUTDATA (Structure List)
    /// </summary>
    public partial class CBR_SET_EOL_2D_PRINT_VERIFICATION_OUTList : List<CBR_SET_EOL_2D_PRINT_VERIFICATION_OUT>
    {
        public CBR_SET_EOL_2D_PRINT_VERIFICATION_OUTList(List<CStructureVariableCollection> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Add(new CBR_SET_EOL_2D_PRINT_VERIFICATION_OUT(list[i]));
            }

        }

        public static CBR_SET_EOL_2D_PRINT_VERIFICATION_OUTList GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureListVariable(owner, "BR_SET_EOL_2D_PRINT_VERIFICATION_OUT");
            return var != null ? new CBR_SET_EOL_2D_PRINT_VERIFICATION_OUTList(var.StructureList) : null;
        }
    }
    #endregion

    #region CBR_SET_EOL_CELL_OUTPUT_IN 의 요약
    /// <summary>
    /// TABLE_SEQ=INDATA:IN_SUBLOT
    /// </summary>
    public partial class CBR_SET_EOL_CELL_OUTPUT_IN : CStructureVariable
    {
        /// <summary>
        /// Structure item from BizActor[2021-03-29 14:31:45]
        /// </summary>
        public CVariable __INDATA
        {
            get { return _struct["INDATA"]; }
        }

        #region CINDATA 의 요약
        /// <summary>
        /// Structure item from BizActor[2021-03-29 14:31:45]
        /// </summary>
        public partial class CINDATA : CStructureVariable
        {
            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public CVariable __SRCTYPE
            {
                get { return _struct["SRCTYPE"]; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public string SRCTYPE
            {
                get { return _struct["SRCTYPE"].AsString; }
                set { _struct["SRCTYPE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public CVariable __IFMODE
            {
                get { return _struct["IFMODE"]; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public string IFMODE
            {
                get { return _struct["IFMODE"].AsString; }
                set { _struct["IFMODE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public CVariable __EQPTID
            {
                get { return _struct["EQPTID"]; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public string EQPTID
            {
                get { return _struct["EQPTID"].AsString; }
                set { _struct["EQPTID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public CVariable __USERID
            {
                get { return _struct["USERID"]; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public string USERID
            {
                get { return _struct["USERID"].AsString; }
                set { _struct["USERID"].AsString = value; }
            }

            public CINDATA(CStructureVariableCollection structure) : base(structure)
            {
            }

            public static CINDATA GetNew(CObject owner)
            {
                CVariable var = CStructureVariable.CreateStructureVariable(owner, "INDATA", "Structure item from BizActor[2021-03-29 14:31:45]");
                return var != null ? new CINDATA(var.Structure) : null;
            }
        }
        #endregion
        /// <summary>
        /// Structure item from BizActor[2021-03-29 14:31:45]
        /// </summary>
        public List<CINDATA> INDATA = new List<CINDATA>();
        /// <summary>
        /// Get, Set INDATA ListLength
        /// </summary>
        public int INDATA_LENGTH
        {
            get { return GetINDATALength(); }
            set { __INDATA.Length = value; }
        }
        private int GetINDATALength()
        {
            if (INDATA.Count != _struct["INDATA"].StructureList.Count)
            {
                UpdateINDATALength();
            }
            return INDATA.Count;
        }
        private void UpdateINDATALength()
        {
            INDATA.Clear();
            for (int i = 0; i < _struct["INDATA"].StructureList.Count; i++)
            {
                INDATA.Add(new CINDATA(_struct["INDATA"].StructureList[i]));
            }
        }
        /// <summary>
        /// Structure item from BizActor[2021-03-29 14:31:45]
        /// </summary>
        public CVariable __IN_SUBLOT
        {
            get { return _struct["IN_SUBLOT"]; }
        }

        #region CIN_SUBLOT 의 요약
        /// <summary>
        /// Structure item from BizActor[2021-03-29 14:31:45]
        /// </summary>
        public partial class CIN_SUBLOT : CStructureVariable
        {
            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public CVariable __SUBLOTID
            {
                get { return _struct["SUBLOTID"]; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public string SUBLOTID
            {
                get { return _struct["SUBLOTID"].AsString; }
                set { _struct["SUBLOTID"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public CVariable __EQPT_DFCT_CODE
            {
                get { return _struct["EQPT_DFCT_CODE"]; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public string EQPT_DFCT_CODE
            {
                get { return _struct["EQPT_DFCT_CODE"].AsString; }
                set { _struct["EQPT_DFCT_CODE"].AsString = value; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public CVariable __OUTPUT_RSLT_INFO
            {
                get { return _struct["OUTPUT_RSLT_INFO"]; }
            }

            /// <summary>
            /// String item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public string OUTPUT_RSLT_INFO
            {
                get { return _struct["OUTPUT_RSLT_INFO"].AsString; }
                set { _struct["OUTPUT_RSLT_INFO"].AsString = value; }
            }

            public CIN_SUBLOT(CStructureVariableCollection structure) : base(structure)
            {
            }

            public static CIN_SUBLOT GetNew(CObject owner)
            {
                CVariable var = CStructureVariable.CreateStructureVariable(owner, "IN_SUBLOT", "Structure item from BizActor[2021-03-29 14:31:45]");
                return var != null ? new CIN_SUBLOT(var.Structure) : null;
            }
        }
        #endregion
        /// <summary>
        /// Structure item from BizActor[2021-03-29 14:31:45]
        /// </summary>
        public List<CIN_SUBLOT> IN_SUBLOT = new List<CIN_SUBLOT>();
        /// <summary>
        /// Get, Set IN_SUBLOT ListLength
        /// </summary>
        public int IN_SUBLOT_LENGTH
        {
            get { return GetIN_SUBLOTLength(); }
            set { __IN_SUBLOT.Length = value; }
        }
        private int GetIN_SUBLOTLength()
        {
            if (IN_SUBLOT.Count != _struct["IN_SUBLOT"].StructureList.Count)
            {
                UpdateIN_SUBLOTLength();
            }
            return IN_SUBLOT.Count;
        }
        private void UpdateIN_SUBLOTLength()
        {
            IN_SUBLOT.Clear();
            for (int i = 0; i < _struct["IN_SUBLOT"].StructureList.Count; i++)
            {
                IN_SUBLOT.Add(new CIN_SUBLOT(_struct["IN_SUBLOT"].StructureList[i]));
            }
        }
        public CBR_SET_EOL_CELL_OUTPUT_IN(CStructureVariableCollection structure) : base(structure)
        {
            _struct["INDATA"].StructureList.OnListLengthUpdated += new LGCNS.ezControl.Common.delegateDo(UpdateINDATALength);
            UpdateINDATALength();

            _struct["IN_SUBLOT"].StructureList.OnListLengthUpdated += new LGCNS.ezControl.Common.delegateDo(UpdateIN_SUBLOTLength);
            UpdateIN_SUBLOTLength();

        }

        public static CBR_SET_EOL_CELL_OUTPUT_IN GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureVariable(owner, "BR_SET_EOL_CELL_OUTPUT_IN", "TABLE_SEQ=INDATA:IN_SUBLOT");
            return var != null ? new CBR_SET_EOL_CELL_OUTPUT_IN(var.Structure) : null;
        }
    }
    #endregion
    #region CBR_SET_EOL_CELL_OUTPUT_INList 의 요약
    /// <summary>
    /// TABLE_SEQ=INDATA:IN_SUBLOT (Structure List)
    /// </summary>
    public partial class CBR_SET_EOL_CELL_OUTPUT_INList : List<CBR_SET_EOL_CELL_OUTPUT_IN>
    {
        public CBR_SET_EOL_CELL_OUTPUT_INList(List<CStructureVariableCollection> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Add(new CBR_SET_EOL_CELL_OUTPUT_IN(list[i]));
            }

        }

        public static CBR_SET_EOL_CELL_OUTPUT_INList GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureListVariable(owner, "BR_SET_EOL_CELL_OUTPUT_IN");
            return var != null ? new CBR_SET_EOL_CELL_OUTPUT_INList(var.StructureList) : null;
        }
    }
    #endregion
    #region CBR_SET_EOL_CELL_OUTPUT_OUT 의 요약
    /// <summary>
    /// TABLE_SEQ=OUTDATA
    /// </summary>
    public partial class CBR_SET_EOL_CELL_OUTPUT_OUT : CStructureVariable
    {
        /// <summary>
        /// Structure item from BizActor[2021-03-29 14:31:45]
        /// </summary>
        public CVariable __OUTDATA
        {
            get { return _struct["OUTDATA"]; }
        }

        #region COUTDATA 의 요약
        /// <summary>
        /// Structure item from BizActor[2021-03-29 14:31:45]
        /// </summary>
        public partial class COUTDATA : CStructureVariable
        {
            /// <summary>
            /// Long item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public CVariable __RETVAL
            {
                get { return _struct["RETVAL"]; }
            }

            /// <summary>
            /// Long item from BizActor[2021-03-29 14:31:45]
            /// </summary>
            public long RETVAL
            {
                get { return _struct["RETVAL"].AsLong; }
                set { _struct["RETVAL"].AsLong = value; }
            }

            public COUTDATA(CStructureVariableCollection structure) : base(structure)
            {
            }

            public static COUTDATA GetNew(CObject owner)
            {
                CVariable var = CStructureVariable.CreateStructureVariable(owner, "OUTDATA", "Structure item from BizActor[2021-03-29 14:31:45]");
                return var != null ? new COUTDATA(var.Structure) : null;
            }
        }
        #endregion
        /// <summary>
        /// Structure item from BizActor[2021-03-29 14:31:45]
        /// </summary>
        public List<COUTDATA> OUTDATA = new List<COUTDATA>();
        /// <summary>
        /// Get, Set OUTDATA ListLength
        /// </summary>
        public int OUTDATA_LENGTH
        {
            get { return GetOUTDATALength(); }
            set { __OUTDATA.Length = value; }
        }
        private int GetOUTDATALength()
        {
            if (OUTDATA.Count != _struct["OUTDATA"].StructureList.Count)
            {
                UpdateOUTDATALength();
            }
            return OUTDATA.Count;
        }
        private void UpdateOUTDATALength()
        {
            OUTDATA.Clear();
            for (int i = 0; i < _struct["OUTDATA"].StructureList.Count; i++)
            {
                OUTDATA.Add(new COUTDATA(_struct["OUTDATA"].StructureList[i]));
            }
        }
        public CBR_SET_EOL_CELL_OUTPUT_OUT(CStructureVariableCollection structure) : base(structure)
        {
            _struct["OUTDATA"].StructureList.OnListLengthUpdated += new LGCNS.ezControl.Common.delegateDo(UpdateOUTDATALength);
            UpdateOUTDATALength();

        }

        public static CBR_SET_EOL_CELL_OUTPUT_OUT GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureVariable(owner, "BR_SET_EOL_CELL_OUTPUT_OUT", "TABLE_SEQ=OUTDATA");
            return var != null ? new CBR_SET_EOL_CELL_OUTPUT_OUT(var.Structure) : null;
        }
    }
    #endregion
    #region CBR_SET_EOL_CELL_OUTPUT_OUTList 의 요약
    /// <summary>
    /// TABLE_SEQ=OUTDATA (Structure List)
    /// </summary>
    public partial class CBR_SET_EOL_CELL_OUTPUT_OUTList : List<CBR_SET_EOL_CELL_OUTPUT_OUT>
    {
        public CBR_SET_EOL_CELL_OUTPUT_OUTList(List<CStructureVariableCollection> list)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Add(new CBR_SET_EOL_CELL_OUTPUT_OUT(list[i]));
            }

        }

        public static CBR_SET_EOL_CELL_OUTPUT_OUTList GetNew(CObject owner)
        {
            CVariable var = CStructureVariable.CreateStructureListVariable(owner, "BR_SET_EOL_CELL_OUTPUT_OUT");
            return var != null ? new CBR_SET_EOL_CELL_OUTPUT_OUTList(var.StructureList) : null;
        }
    }
    #endregion
}