from training.train_model import train_turbine_from_rows


def test_training_requires_enough_rows():
    try:
        train_turbine_from_rows('T-1', rows=[])
    except ValueError:
        assert True
    else:
        assert False
