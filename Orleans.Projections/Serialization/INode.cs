using System.Linq.Expressions;

namespace Orleans.Projections.Serialization;

public interface INode
{
	Expression BuildExpression (ParameterExpression parameter);
}