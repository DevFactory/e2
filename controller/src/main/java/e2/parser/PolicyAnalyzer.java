package e2.parser;

import org.antlr.v4.runtime.tree.ParseTreeProperty;

import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;
import java.util.stream.Collectors;

import e2.parser.PolicyParser.DefinitionContext;
import e2.parser.PolicyParser.EdgeContext;
import e2.parser.PolicyParser.PipelineContext;
import e2.parser.PolicyParser.VertexContext;
import e2.parser.PolicyParser.VertexListContext;
import e2.pipelet.Edge;
import e2.pipelet.PipeletType;
import e2.pipelet.Vertex;

public class PolicyAnalyzer extends PolicyBaseListener {
    private final List<Vertex> vertices = new ArrayList<>();
    private final Map<String, Vertex> vertexMap = new HashMap<>();
    private final List<PipeletType> types = new ArrayList<>();
    private final ParseTreeProperty<Edge> edgeProps = new ParseTreeProperty<>();
    private final ParseTreeProperty<Endpoint> vertexProps = new ParseTreeProperty<>();
    private final ParseTreeProperty<List<Endpoint>> vertexListProps = new ParseTreeProperty<>();

    @Override
    public void exitDefinition(DefinitionContext ctx) {
        Vertex v = new Vertex(ctx.type.getText(), ctx.name.getText());
        vertexMap.put(ctx.name.getText(), v);
        vertices.add(v);
    }

    @Override
    public void exitVertex(VertexContext ctx) {
        Vertex v = vertexMap.get(ctx.name.getText());
        int p = Integer.parseInt(ctx.port.getText());
        vertexProps.put(ctx, new Endpoint(v, p));
    }

    @Override
    public void exitVertexList(VertexListContext ctx) {
        List<Endpoint> vertexList = ctx.vertex().stream()
                .map(vertexProps::get)
                .collect(Collectors.toList());
        vertexListProps.put(ctx, vertexList);
    }

    @Override
    public void exitEdge(EdgeContext ctx) {
        Endpoint source = vertexProps.get(ctx.src);
        Endpoint target = vertexProps.get(ctx.dst);
        String filter = (ctx.filter == null)
                ? ""
                : ctx.filter.getText().substring(2, ctx.filter.getText().length() - 2);
        Edge e = new Edge(source.v, target.v, source.p, target.p, filter);
        edgeProps.put(ctx, e);
    }

    @Override
    public void exitPipeline(PipelineContext ctx) {
        List<Edge> edges = ctx.edgeList().edge().stream()
                .map(edgeProps::get)
                .collect(Collectors.toList());
        String filter = (ctx.filter == null)
                ? ""
                : ctx.filter.getText().substring(1, ctx.filter.getText().length() - 1);
        PipeletType type = new PipeletType(vertices, edges, filter);

        Endpoint inf = vertexProps.get(ctx.inf);
        type.addForwardEntryPoint(inf.v, inf.p, "");

        Endpoint inr = vertexProps.get(ctx.inr);
        type.addReverseEntryPoint(inr.v, inr.p, "");

        vertexListProps.get(ctx.out).stream()
                .forEach(endpoint -> type.addExitPoint(endpoint.v, endpoint.p));

        types.add(type);
    }

    public List<PipeletType> PipeletTypes() {
        return new ArrayList<>(types);
    }

    private class Endpoint {
        public Vertex v;
        public int p;

        public Endpoint(Vertex v, int p) {
            this.v = v;
            this.p = p;
        }
    }
}
