﻿/*
 * Authors:
 *   钟峰(Popeye Zhong) <zongsoft@gmail.com>
 *
 * Copyright (C) 2016 Zongsoft Corporation <http://www.zongsoft.com>
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
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace Zongsoft.Data
{
	/// <summary>
	/// 表示数据过滤条件的组合实体。
	/// </summary>
	public abstract class Conditional : Zongsoft.ComponentModel.NotifyObject, IConditional
	{
		#region 静态变量
		private static readonly ConcurrentDictionary<Type, ConditionalDescriptor> _cache = new ConcurrentDictionary<Type, ConditionalDescriptor>();
		#endregion

		#region 成员字段
		private ConditionCombination _conditionCombination;
		#endregion

		#region 构造函数
		protected Conditional()
		{
			_conditionCombination = ConditionCombination.And;
		}
		#endregion

		#region 保护属性
		protected ConditionCombination ConditionCombination
		{
			get
			{
				return _conditionCombination;
			}
			set
			{
				_conditionCombination = value;
			}
		}
		#endregion

		#region 符号重写
		public static implicit operator ConditionCollection(Conditional conditional)
		{
			if(conditional == null)
				return null;

			return conditional.ToConditions();
		}

		public static ConditionCollection operator &(Condition condition, Conditional conditional)
		{
			if(conditional == null)
				return null;

			return condition & conditional.ToConditions();
		}

		public static ConditionCollection operator &(Conditional conditional, Condition condition)
		{
			if(conditional == null)
				return null;

			return conditional.ToConditions() & condition;
		}

		public static ConditionCollection operator &(Conditional left, Conditional right)
		{
			if(left == null)
				return right;

			if(right == null)
				return left;

			return left.ToConditions() & right.ToConditions();
		}

		public static ConditionCollection operator |(Condition condition, Conditional conditional)
		{
			if(conditional == null)
				return null;

			return condition | conditional.ToConditions();
		}

		public static ConditionCollection operator |(Conditional conditional, Condition condition)
		{
			if(conditional == null)
				return null;

			return conditional.ToConditions() | condition;
		}

		public static ConditionCollection operator |(Conditional left, Conditional right)
		{
			if(left == null)
				return right;

			if(right == null)
				return left;

			return left.ToConditions() | right.ToConditions();
		}
		#endregion

		#region 公共方法
		public virtual ConditionCollection ToConditions()
		{
			ConditionCollection conditions = null;
			var descriptor = _cache.GetOrAdd(this.GetType(), type => new ConditionalDescriptor(type));

			foreach(var property in descriptor.Properties)
			{
				var condition = this.GenerateCondition(property);

				if(condition != null)
				{
					if(conditions == null)
						conditions = new ConditionCollection(this.ConditionCombination);

					conditions.Add(condition);
				}
			}

			return conditions;
		}
		#endregion

		#region 私有方法
		private ICondition GenerateCondition(ConditionalPropertyDescripor property)
		{
			//如果当前属性值为默认值，则忽略它
			if(property == null)
				return null;

			//获取当前属性对应的条件命列表
			var names = this.GetConditionNames(property);

			//创建转换器上下文
			var context = new ConditionalConverterContext(this, names, property.PropertyType, property.GetValue(this), property.Operator, property.DefaultValue);

			//如果当前属性指定了特定的转换器，则使用该转换器来处理
			if(property.Converter != null)
				return property.Converter.Convert(context);

			//使用默认转换器进行转换处理
			return ConditionalConverter.Default.Convert(context);
		}

		private string[] GetConditionNames(ConditionalPropertyDescripor property)
		{
			if(property.Attribute != null && property.Attribute.Names != null && property.Attribute.Names.Length > 0)
				return property.Attribute.Names;

			return new string[] { property.Name };
		}
		#endregion

		#region 嵌套子类
		private class ConditionalDescriptor
		{
			public readonly Type Type;
			public readonly ConditionalPropertyDescripor[] Properties;

			public ConditionalDescriptor(Type type)
			{
				this.Type = type;

				var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
				var list = new List<ConditionalPropertyDescripor>(properties.Length);

				foreach(var property in properties)
				{
					if(!property.CanRead)
						continue;

					var attribute = property.GetCustomAttribute<ConditionalAttribute>(true);

					if(attribute != null && attribute.Ignored)
						continue;

					list.Add(new ConditionalPropertyDescripor(property, attribute));
				}

				this.Properties = list.ToArray();
			}
		}

		private class ConditionalPropertyDescripor
		{
			public readonly string Name;
			public readonly Type PropertyType;
			public readonly PropertyInfo Info;
			public readonly ConditionalAttribute Attribute;
			public readonly object DefaultValue;
			public readonly IConditionalConverter Converter;

			public ConditionalPropertyDescripor(PropertyInfo property, ConditionalAttribute attribute)
			{
				this.Info = property;
				this.Attribute = attribute;
				this.Name = property.Name;
				this.PropertyType = property.PropertyType;

				var defaultAttribute = property.GetCustomAttribute<System.ComponentModel.DefaultValueAttribute>(true);

				if(defaultAttribute != null)
					this.DefaultValue = Convert.IsDBNull(defaultAttribute.Value) ? null : defaultAttribute.Value;

				if(attribute != null && attribute.ConverterType != null)
					this.Converter = Activator.CreateInstance(attribute.ConverterType) as IConditionalConverter;
			}

			public ConditionOperator? Operator
			{
				get
				{
					return this.Attribute != null ? this.Attribute.Operator : null;
				}
			}

			public object GetValue(object target)
			{
				return this.Info.GetValue(target);
			}
		}
		#endregion
	}
}
