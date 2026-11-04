using System.Text.RegularExpressions;

public class AccountIdSpecifiction
{
    private static readonly Regex IdRegex = new Regex(
        @"^(?=.{3,20}$)[A-Za-z0-9_+-]+(\.[A-Za-z0-9_+-]+)*@[A-Za-z0-9-]+(\.[A-Za-z0-9-]+)+$");

    public string ErrorMessage => "아이디는 이메일 형식이어야 합니다.";

    public bool IsSatisfiedBy(string email)
    {
        return !IdRegex.IsMatch(email);
    }
}