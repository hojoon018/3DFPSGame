using System.Text.RegularExpressions;

public class AccountPasswordSpecification
{
    private static readonly Regex PasswordRegex = new Regex(
        @"^(?=.*[a-z])(?=.*[0-9])(?=.*[^A-Za-z0-9])[!-~]{10,30}$");

    public string ErrorMessage => "비밀번호는 소문자, 숫자, 기호를 모두 포함한 10~30 글자여야 합니다.";

    public bool IsSatisfiedBy(string password)
    {
        return PasswordRegex.IsMatch(password);
    }
}