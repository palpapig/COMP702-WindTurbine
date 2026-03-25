from app.services.alarm_service import alarm_service


def test_alarm_service_returns_alarm_info():
    alarm = alarm_service.evaluate('T-TEST', residual=10.0, residual_std=1.0)
    assert alarm is not None
