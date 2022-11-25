using Nml.Refactor.Me.Dependencies;
using Nml.Refactor.Me.MessageBuilders;
using System;
using System.Threading.Tasks;

namespace Nml.Refactor.Me.Notifiers
{
	public class SmsNotifier : INotifier
	{
		private readonly IStringMessageBuilder _messageBuilder;
		private readonly IOptions _options;
		private readonly ILogger _logger = LogManager.For<SmsNotifier>();

		public SmsNotifier(IStringMessageBuilder messageBuilder, IOptions options)
		{
			_messageBuilder = messageBuilder ?? throw new ArgumentNullException(nameof(messageBuilder));
			_options = options ?? throw new ArgumentNullException(nameof(options));
		}

		public async Task Notify(NotificationMessage message)
		{
			var smsClient = new SmsApiClient(_options.Sms.ApiUri, _options.Sms.ApiKey);
			var smsMessage = _messageBuilder.CreateMessage(message);

			try
			{
				await smsClient.SendAsync(message.MobileNumber, smsMessage);
				_logger.LogTrace($"Message sent.");
			}
			catch (Exception e)
			{
				_logger.LogError(e, $"Failed to send message. {e.Message}");
				throw;
			}
		}
	}
}
