﻿/*
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2010-2013 Zongsoft Corporation <http://www.zongsoft.com>
 *
 * This file is part of Zongsoft.CoreLibrary.
 *
 * Zongsoft.CoreLibrary is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 *
 * Zongsoft.CoreLibrary is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * The above copyright notice and this permission notice shall be
 * included in all copies or substantial portions of the Software.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with Zongsoft.CoreLibrary; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
 */

using System;
using System.Collections.Generic;
using System.Text;

namespace Zongsoft.Services
{
	[Serializable]
	public class CommandExecutingEventArgs : EventArgs
	{
		#region 成员变量
		private CommandContextBase _context;
		private object _parameter;
		private IDictionary<string, object> _extendedProperties;
		private object _result;
		private bool _cancel;
		#endregion

		#region 构造函数
		public CommandExecutingEventArgs(CommandContextBase context, bool cancel = false)
		{
			if(context != null)
			{
				_context = context;
				_parameter = context.Parameter;
				_extendedProperties = context.HasExtendedProperties ? context.ExtendedProperties : null;
				_result = context.Result;
			}

			_cancel = cancel;
		}

		public CommandExecutingEventArgs(object parameter, IDictionary<string, object> extendedProperties, bool cancel = false)
		{
			var context = parameter as CommandContextBase;

			if(context != null)
			{
				_context = context;
				_parameter = context.Parameter;
				_extendedProperties = context.HasExtendedProperties ? context.ExtendedProperties : null;
				_result = context.Result;
			}
			else
			{
				_parameter = parameter;
				_extendedProperties = extendedProperties;
			}

			_cancel = cancel;
		}
		#endregion

		#region 公共属性
		/// <summary>
		/// 获取或设置一个值，表示是否取消当前命令的执行。
		/// </summary>
		public bool Cancel
		{
			get
			{
				return _cancel;
			}
			set
			{
				_cancel = value;
			}
		}

		/// <summary>
		/// 获取命令的执行上下文对象。
		/// </summary>
		public CommandContextBase Context
		{
			get
			{
				return _context;
			}
		}

		/// <summary>
		/// 获取命令的执行参数对象。
		/// </summary>
		public object Parameter
		{
			get
			{
				return _parameter;
			}
		}

		public object Result
		{
			get
			{
				return _result;
			}
			set
			{
				_result = value;

				if(_context != null)
					_context.Result = value;
			}
		}

		public bool HasExtendedProperties
		{
			get
			{
				return _extendedProperties != null && _extendedProperties.Count > 0;
			}
		}

		/// <summary>
		/// 获取可用于在命令执行过程中在各处理模块之间组织和共享数据的键/值集合。
		/// </summary>
		public IDictionary<string, object> ExtendedProperties
		{
			get
			{
				if(_extendedProperties == null)
					System.Threading.Interlocked.CompareExchange(ref _extendedProperties, new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase), null);

				return _extendedProperties;
			}
		}
		#endregion
	}
}
