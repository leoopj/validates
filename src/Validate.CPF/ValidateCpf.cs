using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ValidateCpf
{
    public class ValidateCpf
    {
        private readonly ILogger<ValidateCpf> _logger;
        public ValidateCpf(ILogger<ValidateCpf> logger)
             => _logger = logger;

        [Function("validate-cpf")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
        {
            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic? data = JsonConvert.DeserializeObject(requestBody);
                string? cpf = data?.cpf;

                if (string.IsNullOrEmpty(cpf))
                {
                    _logger.LogError("CPF não informado.");
                    return new BadRequestObjectResult("Por favor informe o CPF.");
                }

                var isValid = await CpfIsValid(cpf);

                if (isValid)
                    return new OkObjectResult("CPF válido.");
                else
                    return new OkObjectResult("CPF inválido.");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Erro na validação de CPF: {ex.Message}");
                throw new Exception("Ocorreu um erro no sistema: ", ex);
            }
        }

        private async Task<bool> CpfIsValid(string cpf)
        {
            cpf = cpf.Trim().Replace(".", "").Replace("-", "");
            if (cpf.Length != 11)
                return false;

            bool allDigitsEqual = true;
            for (int i = 1; i < cpf.Length; i++)
            {
                if (cpf[i] != cpf[0])
                {
                    allDigitsEqual = false;
                    break;
                }
            }
            if (allDigitsEqual)
                return false;

            int sum = 0;
            for (int i = 0; i < 9; i++)
                sum += int.Parse(cpf[i].ToString()) * (10 - i);

            int remainder = sum % 11;
            int digit1 = remainder < 2 ? 0 : 11 - remainder;

            sum = 0;
            for (int i = 0; i < 10; i++)
                sum += int.Parse(cpf[i].ToString()) * (11 - i);

            remainder = sum % 11;
            int digit2 = remainder < 2 ? 0 : 11 - remainder;

            return await Task.FromResult(cpf[9] == digit1.ToString()[0] && cpf[10] == digit2.ToString()[0]);
        }
    }
}
