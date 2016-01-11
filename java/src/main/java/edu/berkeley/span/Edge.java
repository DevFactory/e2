package edu.berkeley.span;
import edu.berkeley.span.Vertex;
public final class Edge {
	public Vertex src;
	public Vertex dst;
	public Edge(Vertex src, Vertex dst) {
		this.src = src;
		this.dst = dst;
	}

	@Override
	public int hashCode() {
		return src.hashCode() + dst.hashCode();
	}
}
