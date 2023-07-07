package main

import (
	"encoding/json"
	"log"
	"net/http"

	_ "velaboratory/velconnect/migrations"

	"github.com/labstack/echo/v5"
	"github.com/pocketbase/pocketbase"
	"github.com/pocketbase/pocketbase/apis"
	"github.com/pocketbase/pocketbase/core"
	"github.com/pocketbase/pocketbase/models"
	"github.com/pocketbase/pocketbase/plugins/migratecmd"
)

func main() {
	app := pocketbase.New()

	// loosely check if it was executed using "go run"
	// isGoRun := strings.HasPrefix(os.Args[0], os.TempDir())

	migratecmd.MustRegister(app, app.RootCmd, &migratecmd.Options{
		// enable auto creation of migration files when making collection changes
		// (the isGoRun check is to enable it only during development)
		Automigrate: true,
	})

	app.OnBeforeServe().Add(func(e *core.ServeEvent) error {
		// or you can also use the shorter e.Router.GET("/articles/:slug", handler, middlewares...)
		e.Router.POST("/data_block/:block_id", func(c echo.Context) error {

			dao := app.Dao()
			requestData := apis.RequestData(c)

			log.Println(requestData)

			// get the old value to do a merge
			record, err := dao.FindFirstRecordByData("DataBlock", "block_id", c.PathParam("block_id"))
			if err == nil {
				mergeDataBlock(requestData, record)
			} else {

				collection, err := dao.FindCollectionByNameOrId("DataBlock")
				if err != nil {
					return err
				}

				record = models.NewRecord(collection)

				// we don't have an existing data, so just set the new values
				if val, ok := requestData.Data["data"]; ok {
					record.Set("data", val)
				}
			}

			// add the new values
			record.Set("block_id", c.PathParam("block_id"))
			fields := []string{
				"owner_id",
				"visibility",
				"category",
			}
			for _, v := range fields {
				if val, ok := requestData.Data[v]; ok {
					record.Set(v, val)
				}
			}

			// apply to the db
			if err := dao.SaveRecord(record); err != nil {
				return err
			}

			log.Println(record)
			return c.JSON(http.StatusOK, record)
		},
			apis.ActivityLogger(app),
		)

		e.Router.GET("/data_block/:block_id", func(c echo.Context) error {
			record, err := app.Dao().FindFirstRecordByData("DataBlock", "block_id", c.PathParam("block_id"))
			if err != nil {
				return apis.NewNotFoundError("The data block does not exist.", err)
			}

			// enable ?expand query param support
			apis.EnrichRecord(c, app.Dao(), record)

			return c.JSON(http.StatusOK, record)
		},
			apis.ActivityLogger(app),
		)

		e.Router.POST("/device/:device_id", func(c echo.Context) error {

			dao := app.Dao()
			requestData := apis.RequestData(c)

			// get the old value to do a merge
			record, err := dao.FindFirstRecordByData("Device", "device_id", c.PathParam("device_id"))
			if err == nil {
				mergeDataBlock(requestData, record)
			} else {

				collection, err := dao.FindCollectionByNameOrId("Device")
				if err != nil {
					return err
				}

				record = models.NewRecord(collection)

				// we don't have an existing data, so just set the new values
				if val, ok := requestData.Data["data"]; ok {
					record.Set("data", val)
				}
			}

			// add the new values
			record.Set("device_id", c.PathParam("device_id"))
			fields := []string{
				"os_info",
				"friendly_name",
				"modified_by",
				"current_app",
				"current_room",
				"pairing_code",
				"last_online",
			}
			for _, v := range fields {
				if val, ok := requestData.Data[v]; ok {
					record.Set(v, val)
				}
			}

			// apply to the db
			if err := dao.SaveRecord(record); err != nil {
				return err
			}

			return c.JSON(http.StatusOK, record)
		},
			apis.ActivityLogger(app),
		)

		e.Router.GET("/device/:device_id", func(c echo.Context) error {
			record, err := app.Dao().FindFirstRecordByData("Device", "device_id", c.PathParam("device_id"))
			if err != nil {
				return apis.NewNotFoundError("The device does not exist.", err)
			}

			// enable ?expand query param support
			apis.EnrichRecord(c, app.Dao(), record)

			return c.JSON(http.StatusOK, record)
		},
			apis.ActivityLogger(app),
		)

		// gets all relevant tables for this device id
		e.Router.GET("/state/device/:device_id", func(c echo.Context) error {
			record, err := app.Dao().FindFirstRecordByData("Device", "device_id", c.PathParam("device_id"))
			if err != nil {
				return apis.NewNotFoundError("The device does not exist.", err)
			}

			room, _ := app.Dao().FindFirstRecordByData("DataBlock", "block_id", record.GetString("current_room"))

			output := map[string]interface{}{
				"device": record,
				"room":   room,
			}

			return c.JSON(http.StatusOK, output)
		},
			apis.ActivityLogger(app),
		)

		return nil
	})

	if err := app.Start(); err != nil {
		log.Fatal(err)
	}
}

func mergeDataBlock(requestData *models.RequestData, record *models.Record) {

	// get the new data
	newData, hasNewData := requestData.Data["data"]
	if hasNewData {
		// convert the existing data to a map
		var newDataMap = map[string]interface{}{}
		for k, v := range newData.(map[string]interface{}) {
			newDataMap[k] = v
		}
		// get the existing data
		existingDataString := record.GetString("data")
		existingDataMap := map[string]interface{}{}
		json.Unmarshal([]byte(existingDataString), &existingDataMap)

		// merge the new keys
		// this is only single-level
		for k, v := range newDataMap {
			existingDataMap[k] = v
		}

		record.Set("data", existingDataMap)
	}
}
