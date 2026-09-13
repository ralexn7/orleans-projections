using System.Linq.Expressions;

namespace Orleans.Projections.Nodes;

public interface INode
{
	Expression BuildExpression (BuildContext context);
}