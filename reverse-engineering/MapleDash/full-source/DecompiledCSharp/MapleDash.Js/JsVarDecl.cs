using System.Collections.Generic;

namespace MapleDash.Js;

public sealed class JsVarDecl : JsNode
{
	public readonly List<JsVarDeclarator> Decls = new List<JsVarDeclarator>();

	public override void Exec(JsInterpreter it, JsScope scope)
	{
		for (int i = 0; i < Decls.Count; i++)
		{
			JsVarDeclarator jsVarDeclarator = Decls[i];
			if (jsVarDeclarator.Init == null)
			{
				if (jsVarDeclarator.Pattern != null)
				{
					throw new JsRuntimeException("SyntaxError: destructuring declaration requires initializer");
				}
				if (!scope.FunctionScope.HasOwn(jsVarDeclarator.Name))
				{
					scope.DeclareVar(jsVarDeclarator.Name, JsUndefined.Value);
				}
			}
			else if (jsVarDeclarator.Pattern != null)
			{
				JsPatterns.Bind(jsVarDeclarator.Pattern, jsVarDeclarator.Init.Eval(it, scope), it, scope, declare: true);
			}
			else
			{
				scope.DeclareVar(jsVarDeclarator.Name, jsVarDeclarator.Init.Eval(it, scope));
			}
		}
	}
}
