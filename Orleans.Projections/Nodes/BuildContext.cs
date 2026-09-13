using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

public class BuildContext (ParameterExpression root, object?[] buildingParameters)
{
	public LambdaScope? CurrentLambda { get; private set; }
	
	public ParameterExpression Root => root;

	public object?[] BuildingParameters => buildingParameters;
	
	public IDisposable DeclareLambdaScope (LambdaScopeParameter[] parameters)
	{
		CurrentLambda = new LambdaScope(this, parameters);
		return CurrentLambda;
	}
	
	public record LambdaScopeParameter (string Id, ParameterExpression Parameter);
	public class LambdaScope : IDisposable
	{
		private readonly BuildContext _context;
		private readonly Dictionary<string, ParameterExpression> _parameters;
		
		private readonly LambdaScope? _outerScope;

		public LambdaScope (BuildContext context, LambdaScopeParameter[] parameters)
		{
			_outerScope = context.CurrentLambda;
			_context = context;
			_parameters = parameters.ToDictionary(p => p.Id, p => p.Parameter);
		}
		
		public ParameterExpression GetParameter (string id)
		{
			if (!_parameters.TryGetValue(id, out var parameter))
				throw new InvalidOperationException($"Parameter with id '{id}' not found in the current lambda scope.");
			
			return parameter;
		}

		public void Dispose ()
		{
			_context.CurrentLambda = _outerScope;
		}
	}

}