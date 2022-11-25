using Nml.Refactor.Me.Dependencies;
using Nml.Refactor.Me.MessageBuilders;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Nml.Refactor.Me.Notifiers
{
    public class Notifier : INotifier
    {
        private readonly IOptions _options;
        private readonly ILogger _logger = LogManager.For<Notifier>();
        private readonly IMailMessageBuilder? _mailMessageBuilder;
        private readonly IWebhookMessageBuilder? _slackMessageBuilder;
        private readonly IWebhookMessageBuilder? _teamsMessageBuilder;
        private readonly IStringMessageBuilder? _smsMessageBuilder;

        public Notifier(
            IMailMessageBuilder? mailMessageBuilder,
            IWebhookMessageBuilder? slackMessageBuilder,
            IWebhookMessageBuilder? teamsMessageBuilder,
            IStringMessageBuilder? smsMessageBuilder,
            IOptions options)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));

            if (mailMessageBuilder == null && slackMessageBuilder == null && teamsMessageBuilder == null && smsMessageBuilder == null)
                throw new ArgumentNullException($"{nameof(mailMessageBuilder)} {nameof(slackMessageBuilder)} {nameof(teamsMessageBuilder)} {nameof(smsMessageBuilder)}");

            _mailMessageBuilder = mailMessageBuilder;
            _slackMessageBuilder = slackMessageBuilder;
            _teamsMessageBuilder = teamsMessageBuilder;
            _smsMessageBuilder = smsMessageBuilder;

        }

        public async Task Notify(NotificationMessage message)
        {
            //Email Notifier
            if (_mailMessageBuilder != null)
            {
                var smtp = new SmtpClient(_options.Email.SmtpServer);
                smtp.Credentials = new NetworkCredential(_options.Email.UserName, _options.Email.Password);
                var mailMessage = _mailMessageBuilder.CreateMessage(message);

                try
                {
                    await smtp.SendMailAsync(mailMessage);
                    _logger.LogTrace($"Email Notifier. Message sent.");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Email Notifier. Failed to send message. {e.Message}");
                    throw;
                }
            }

            //Slack Notifier
            if (_slackMessageBuilder != null)
            {
                var serviceEndPoint = new Uri(_options.Slack.WebhookUri);
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, serviceEndPoint);
                request.Content = new StringContent(
                    _slackMessageBuilder.CreateMessage(message).ToString(),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.SendAsync(request);
                _logger.LogTrace($"Slack Notifier. Message sent. {response.StatusCode} -> {response.Content}");
            }

            //Teams Notifier
            if (_teamsMessageBuilder != null)
            {
                var serviceEndPoint = new Uri(_options.Teams.WebhookUri);
                var client = new HttpClient();
                var request = new HttpRequestMessage(HttpMethod.Post, serviceEndPoint);
                request.Content = new StringContent(
                    _teamsMessageBuilder.CreateMessage(message).ToString(),
                    Encoding.UTF8,
                    "application/json");

                try
                {
                    var response = await client.SendAsync(request);
                    _logger.LogTrace($"Teams Notifier. Message sent. {response.StatusCode} -> {response.Content}");
                }
                catch (AggregateException e)
                {
                    foreach (var exception in e.Flatten().InnerExceptions)
                        _logger.LogError(exception, $"Teams Notifier. Failed to send message. {exception.Message}");

                    throw;
                }
            }

            //SMS Notifier
            if (_smsMessageBuilder != null)
            {
                var smsClient = new SmsApiClient(_options.Sms.ApiUri, _options.Sms.ApiKey);
                var smsMessage = _smsMessageBuilder.CreateMessage(message);

                try
                {
                    await smsClient.SendAsync(message.MobileNumber, smsMessage);
                    _logger.LogTrace($"SMS Notifier. Message sent.");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"SMS Notifier. Failed to send message. {e.Message}");
                    throw;
                }
            }
        }
    }
}
