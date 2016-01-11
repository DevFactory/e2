package edu.berkeley.span;
import edu.berkeley.span.Request.Link.Endpoint;
public final class Vertex {
	private String _nf;
	private Integer _port;
	public Vertex(String nf, int port) {
		_nf = nf;
		_port = port;
	}
	private Vertex(String nf) {
		_nf = nf;
		_port = null;
	}
	public static Vertex ForwardIn() {
		return new Vertex("INF");
	}
	public static Vertex ReverseIn() {
		return new Vertex("INR");
	}
	public static Vertex Out() {
		return new Vertex("OUT");
	}
	public Endpoint Encode() {
		Endpoint.Builder b = Endpoint.newBuilder()
				.setNf(_nf);
		if (_port != null) {
			b.setVport(_port);
		}
		return b.build();
	}

	@Override
	public int hashCode() {
		if (_port == null) {
			return _nf.hashCode();
		} else {
			return _nf.hashCode() + _port.toString().hashCode();
		}
	}
}
