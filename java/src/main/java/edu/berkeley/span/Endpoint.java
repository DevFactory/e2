package edu.berkeley.span;
public final class Endpoint {
	private String _nf;
	private Integer _port;
	public Endpoint(String nf, int port) {
		_nf = nf;
		_port = port;
	}
	private Endpoint(String nf) {
		_nf = nf;
		_port = null;
	}
	public static Endpoint ForwardIn() {
		return new Endpoint("INF");
	}
	public static Endpoint ReverseIn() {
		return new Endpoint("INR");
	}
	public static Endpoint Out() {
		return new Endpoint("OUT");
	}
}
