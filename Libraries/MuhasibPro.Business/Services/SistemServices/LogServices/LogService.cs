using MuhasibPro.Business.Contracts.CommonServices;
using MuhasibPro.Business.Contracts.SistemServices.Authentication;
using MuhasibPro.Business.Contracts.SistemServices.LogServices;
using MuhasibPro.Data.Contracts.Repository.Common;

namespace MuhasibPro.Services.Infrastructure.LogServices
{
    public class LogService : ILogService
    {
        private readonly ISistemLogRepository _sistemLogRepository;
        private readonly IAppLogRepository _appLogRepository;
        private readonly IMessageService _messageService;
        private readonly IAuthenticationService _authenticationService;
        public LogService(ISistemLogRepository sistemLogRepository, IAppLogRepository appLogRepository, IMessageService messageService, IAuthenticationService authenticationService)
        {
            _sistemLogRepository = sistemLogRepository;
            _appLogRepository = appLogRepository;
            _messageService = messageService;
            _authenticationService = authenticationService;
            AppLogService = new AppLogService(
                _appLogRepository,
                _messageService,
                _authenticationService);
            SistemLogService = new SistemLogService(
                _sistemLogRepository,
                _messageService,
                _authenticationService);
        }

        public ISistemLogService SistemLogService { get; }
        public IAppLogService AppLogService { get; }
    }

}
