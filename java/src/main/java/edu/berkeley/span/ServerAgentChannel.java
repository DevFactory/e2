package edu.berkeley.span;
import com.google.protobuf.InvalidProtocolBufferException;
import edu.berkeley.span.Request.Command;
import edu.berkeley.span.Resp.*;
import edu.berkeley.span.SerializeDeserialize;
import edu.berkeley.span.ServerAgentException;
import java.io.IOException;
import java.net.InetSocketAddress;
import java.nio.channels.SocketChannel;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
class ServerAgentChannel {
	private InetSocketAddress _address;
	private SocketChannel _conn;
	private SerializeDeserialize _serde;
	private ByteBuffer _lenBuf;
	private ByteBuffer _respBuf;

	public ServerAgentChannel(String address, int port, SerializeDeserialize serde,
			SocketChannel conn) throws IOException {
		_serde = serde;
		_address = new InetSocketAddress(address, port);
		_conn = conn;
		_conn.connect(_address);
		_lenBuf = ByteBuffer.allocateDirect(4);
		_lenBuf.order(ByteOrder.LITTLE_ENDIAN);
		_respBuf = ByteBuffer.allocate(65535);
	}

	public ServerAgentChannel(String address, int port, SerializeDeserialize serde) throws IOException {
		this(address, port, serde, SocketChannel.open());
	}

	public void SendCommand(Command cmd) throws IOException {
		byte[] serialized = cmd.toByteArray();
		int len = serialized.length;
		_lenBuf.clear();
		_lenBuf.putInt(len);
		_lenBuf.rewind();
		while (_lenBuf.hasRemaining()) {
			_conn.write(_lenBuf);
		}
		System.out.println("Should send " + len + " bytes");
		ByteBuffer obj = ByteBuffer.wrap(serialized);
		while (obj.hasRemaining()) {
			_conn.write(obj);
		}
	}

	public Response GetResponse() throws IOException, ServerAgentException {
		int read = 0;
		int data_size = 0;
		_lenBuf.clear();
		do {
			read += _conn.read(_lenBuf);
		} while (read < 4);
		_lenBuf.rewind();
		read = _lenBuf.getInt();
		data_size = read;
		_respBuf.clear();
		_respBuf.limit(read);
		do {
			read -= _conn.read(_respBuf);
		} while (read > 0);
		_respBuf.rewind();
		_respBuf.rewind();
		Response response = _serde.ParseResponse(_respBuf.array(), data_size);
		if (response.getStatus() == Response.Status.Err) {
			edu.berkeley.span.Resp.Error err = response.getExtension(edu.berkeley.span.Resp.Error.ret);
			int errno = err.getErrno();
			String message = err.getError();
			throw new ServerAgentException(errno, message);
		}
		return response;
	}
}
