package edu.berkeley.span;

public class ServerAgentException extends Exception {
	public int errno;
	public String message;
	public ServerAgentException(int errno, String message) {
		super(Integer.toString(errno) + " " + message);
		this.errno = errno;
		this.message = message;
	}
}
