package edu.berkeley.span;
import com.google.protobuf.InvalidProtocolBufferException;
import edu.berkeley.span.Request.Command;
import edu.berkeley.span.Resp.*;
import edu.berkeley.span.SerializeDeserialize;
import java.io.IOException;
import java.net.InetSocketAddress;
import java.nio.channels.AsynchronousSocketChannel;
import java.nio.channels.CompletionHandler;
import java.nio.ByteBuffer;
import java.nio.ByteOrder;
import java.util.concurrent.ExecutionException;
import edu.berkeley.span.Notification;
import edu.berkeley.span.Notification.Upcall;
class NotificationAgent {
	private static final int DEFAULT_PORT = 10516;
	private static final String DEFAULT_ADDR = "127.0.0.1";
	private SerializeDeserialize _serde;
	private AsynchronousSocketChannel _sock;
	private InetSocketAddress _address;
	private ByteBuffer _lenBuf;
	private ByteBuffer _respBuf;
	public NotificationAgent(String address, int port) throws IOException, ExecutionException {
		_sock = AsynchronousSocketChannel.open();
		_address = new InetSocketAddress(address, port);
		_serde = new SerializeDeserialize();
		_lenBuf = ByteBuffer.allocateDirect(4);
		_lenBuf.order(ByteOrder.LITTLE_ENDIAN);
		_respBuf = ByteBuffer.allocate(65535);
		// Connect synchronously for now. If necessary we can move to more async things here.
		connect();
		sendCommandSync(_serde.RegisterForNotification());
	}

	private void connect() throws IOException, ExecutionException {
		try {
			_sock.connect(_address).get();
		} catch (InterruptedException e) {
			// OK so we are about to die or something.
			throw new IOException(e.toString());
		}
	}

	public NotificationAgent(String address) throws IOException, ExecutionException {
		this(address, DEFAULT_PORT);
	}

	public NotificationAgent() throws IOException, ExecutionException {
		this(DEFAULT_ADDR, DEFAULT_PORT);
	}

	private void sendCommandSync(Command cmd) throws IOException, ExecutionException {
		try {
			byte[] serialized = cmd.toByteArray();
			int len = serialized.length;
			_lenBuf.clear();
			_lenBuf.putInt(len);
			_lenBuf.rewind();
			while (_lenBuf.hasRemaining()) {
				_sock.write(_lenBuf).get();
			}
			ByteBuffer obj = ByteBuffer.wrap(serialized);
			while (obj.hasRemaining()) {
				_sock.write(obj).get();
			}
		} catch (InterruptedException e) {
			// OK so we are about to die or something.
			throw new IOException(e.toString());
		}

	}

	public enum NotificationType {
		Overload, Underload
	}

	public interface NotificationCallback {
		void callback(NotificationAgent agent, NotificationType type, 
				String pipelet_instance, String nf_instance);
	}

	public interface FailureNotification {
		void callback(NotificationAgent agent, Throwable err);
	}

	public void AwaitNotification(NotificationCallback cb, FailureNotification f) {
		_lenBuf.clear();
		_sock.read(_lenBuf, this, 
				new CompletionHandler<Integer, NotificationAgent>() {
					public void completed(Integer result, NotificationAgent agent) {
						try {
							int read = result;
							while (read < 4) {
								read += _sock.read(_lenBuf).get();
							}
							_lenBuf.rewind();
							read = _lenBuf.getInt();
							int data_size = read;
							_respBuf.clear();
							_respBuf.limit(read);
							do {
								read -= _sock.read(_respBuf).get();
							} while (read > 0);
							_respBuf.rewind();
							_respBuf.rewind();
							Upcall notification = _serde.ParseNotification(_respBuf.array(), 
									data_size);
							NotificationType type;
							if (notification.getType() == Upcall.Type.Overload) {
								type = NotificationType.Overload;
							} else {
								type = NotificationType.Underload;
							}
							cb.callback(agent, type, notification.getPipeletId(),
									notification.getNfId());
						} catch (Exception e) {
							f.callback(agent, e);
						} 
					}

					public void failed(Throwable exc, NotificationAgent agent) {
						f.callback(agent, exc);
					}
		});
	}
}
