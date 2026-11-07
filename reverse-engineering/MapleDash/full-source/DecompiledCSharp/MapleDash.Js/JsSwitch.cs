using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsSwitch : JsNode
{
	public JsNode Discriminant;

	public readonly List<JsSwitchCase> Cases = new List<JsSwitchCase>();

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		JsValue a = Discriminant.Eval(it, scope);
		int num = -1;
		int num2 = -1;
		for (int i = 0; i < Cases.Count; i++)
		{
			JsSwitchCase jsSwitchCase = Cases[i];
			if (jsSwitchCase.Test == null)
			{
				num2 = i;
			}
			else if (JsConvert.StrictEquals(a, jsSwitchCase.Test.Eval(it, scope)))
			{
				num = i;
				break;
			}
		}
		if (num < 0)
		{
			num = num2;
		}
		if (num < 0)
		{
			return;
		}
		try
		{
			for (int j = num; j < Cases.Count; j++)
			{
				for (int k = 0; k < Cases[j].Body.Count; k++)
				{
					Cases[j].Body[k].Exec(it, scope);
				}
			}
		}
		catch (JsBreakSignal jsBreakSignal) when (jsBreakSignal.Label == null || jsBreakSignal.Label == Label)
		{
		}
	}
}
