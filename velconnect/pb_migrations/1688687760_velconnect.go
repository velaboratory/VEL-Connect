package migrations

import (
	"github.com/pocketbase/dbx"
	"github.com/pocketbase/pocketbase/daos"
	m "github.com/pocketbase/pocketbase/migrations"
)

func init() {
	m.Register(func(db dbx.Builder) error {

		// set default app settings
		dao := daos.New(db)
		settings, _ := dao.FindSettings()
		settings.Meta.AppName = "VEL-Connect"
		settings.Smtp.Enabled = false
		settings.Logs.MaxDays = 60
		settings.Backups.Cron = "0 0 * * 0"
		settings.Backups.CronMaxKeep = 10
		if err := dao.SaveSettings(settings); err != nil {
			return err
		}

		return nil
	}, func(db dbx.Builder) error {
		// add down queries...

		return nil
	})
}
