using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsForIn : JsNode
{
	public string VarName;

	public JsNode Target;

	public JsNode ObjectExpr;

	public JsNode Body;

	public bool Values;

	public JsNode Pattern;

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		JsValue jsValue = ObjectExpr.Eval(it, scope);
		if (Values)
		{
			foreach (JsValue item in JsPatterns.Iterate(jsValue))
			{
				it.Step();
				if (Pattern != null)
				{
					JsPatterns.Bind(Pattern, item, it, scope, declare: true);
				}
				else if (VarName != null)
				{
					scope.DeclareVar(VarName, item);
				}
				else
				{
					JsPatterns.Bind(Target, item, it, scope, declare: false);
				}
				try
				{
					Body?.Exec(it, scope);
				}
				catch (JsBreakSignal jsBreakSignal) when (jsBreakSignal.Label == null || jsBreakSignal.Label == Label)
				{
					break;
				}
				catch (JsContinueSignal jsContinueSignal) when (jsContinueSignal.Label == null || jsContinueSignal.Label == Label)
				{
				}
			}
			return;
		}
		List<string> list = new List<string>();
		if (jsValue is JsObject jsObject)
		{
			list.AddRange(jsObject.Props.Keys);
		}
		else if (jsValue is JsArray jsArray)
		{
			for (int i = 0; i < jsArray.Items.Count; i++)
			{
				list.Add(i.ToString());
			}
			list.AddRange(jsArray.Props.Keys);
		}
		else if (jsValue is JsHostObject jsHostObject)
		{
			list.AddRange(jsHostObject.Members.Props.Keys);
		}
		else if (jsValue is JsString jsString)
		{
			for (int j = 0; j < jsString.Value.Length; j++)
			{
				list.Add(j.ToString());
			}
		}
		for (int k = 0; k < list.Count; k++)
		{
			it.Step();
			JsString value = new JsString(list[k]);
			if (VarName != null)
			{
				scope.DeclareVar(VarName, value);
			}
			else if (Target is JsIdentifier jsIdentifier)
			{
				scope.Set(jsIdentifier.Name, value);
			}
			else if (Target is JsMember jsMember)
			{
				JsValue obj = jsMember.Object.Eval(it, scope);
				it.SetMember(obj, jsMember.PropertyName(it, scope), value);
			}
			try
			{
				Body?.Exec(it, scope);
			}
			catch (JsBreakSignal jsBreakSignal2) when (jsBreakSignal2.Label == null || jsBreakSignal2.Label == Label)
			{
				break;
			}
			catch (JsContinueSignal jsContinueSignal2) when (jsContinueSignal2.Label == null || jsContinueSignal2.Label == Label)
			{
			}
		}
	}
}
