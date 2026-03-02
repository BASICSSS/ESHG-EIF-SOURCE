/// <summary>
/// LGES.GOCV.CSTBizRule의 요약
/// </summary>
/// <filename>CSTBizRule.cs</filename>
/// <version>1.0.0.0</version>
/// <authors></authors>
/// <modifications>
/// 	1.	 ver1.0.0.0		2020.12.24 11:33:55		Devloper Name	-
/// 		: Created.
/// </modifications>
/// <copyright>Copyright (c) 2007~. EzControl, LG CNS All rights reserved.</copyright>

using System.Collections.Generic;
using LGCNS.ezControl.Core;

namespace ESHG.EIF.FORM.UTIL
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

}