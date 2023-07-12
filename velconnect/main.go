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
			if err != nil {
				// create a new record if needed
				collection, err := dao.FindCollectionByNameOrId("DataBlock")
				if err != nil {
					return err
				}

				record = models.NewRecord(collection)
				record.Set("data", "{}")
			}

			// add the new values
			record.Set("block_id", c.PathParam("block_id"))
			fields := []string{
				"owner",
				"category",
				"modfied_by",
			}
			for _, v := range fields {
				if val, ok := requestData.Data[v]; ok {
					record.Set(v, val)
				}
			}
			mergeDataBlock(requestData, record)

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

		// This is used by Unity itself for device-centric data getting/setting
		e.Router.POST("/device/:device_id", func(c echo.Context) error {

			dao := app.Dao()
			requestData := apis.RequestData(c)

			// get the existing device (by device id)
			deviceRecord, err := dao.FindRecordById("Device", c.PathParam("device_id"))

			// if no device, create one
			if err != nil {
				collection, err := dao.FindCollectionByNameOrId("Device")
				if err != nil {
					log.Fatalln("Couldn't create device")
					return err
				}

				deviceRecord = models.NewRecord(collection)
				deviceRecord.SetId(c.PathParam("device_id"))
			}
			log.Println(deviceRecord.PublicExport())

			// get the device data block
			deviceDataRecord, err := dao.FindRecordById("DataBlock", deviceRecord.GetString("data"))
			if err != nil {
				collection, err := dao.FindCollectionByNameOrId("DataBlock")
				if err != nil {
					log.Fatalln("Couldn't create datablock")
					return err
				}

				deviceDataRecord = models.NewRecord(collection)
				deviceDataRecord.RefreshId()
				deviceRecord.Set("data", deviceDataRecord.Id)
				deviceDataRecord.Set("category", "device")
				deviceDataRecord.Set("data", "{}")
			}

			// add the new device values
			deviceRecord.Set("device_id", c.PathParam("device_id"))
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
					deviceRecord.Set(v, val)
				}
			}

			mergeDataBlock(requestData, deviceDataRecord)

			// apply to the db
			if err := dao.SaveRecord(deviceRecord); err != nil {
				return err
			}
			if err := dao.SaveRecord(deviceDataRecord); err != nil {
				return err
			}

			return c.JSON(http.StatusOK, deviceRecord)
		},
			apis.ActivityLogger(app),
		)

		// e.Router.GET("/device/:device_id", func(c echo.Context) error {
		// 	record, err := app.Dao().FindFirstRecordByData("Device", "device_id", c.PathParam("device_id"))
		// 	if err != nil {
		// 		return apis.NewNotFoundError("The device does not exist.", err)
		// 	}

		// 	// enable ?expand query param support
		// 	apis.EnrichRecord(c, app.Dao(), record)

		// 	return c.JSON(http.StatusOK, record)
		// },
		// 	apis.ActivityLogger(app),
		// )

		// gets all relevant tables for this device id
		e.Router.GET("/state/:device_id", func(c echo.Context) error {
			deviceRecord, err := app.Dao().FindRecordById("Device", c.PathParam("device_id"))
			if err != nil {
				return apis.NewNotFoundError("The device does not exist.", err)
			}

			apis.EnrichRecord(c, app.Dao(), deviceRecord, "data")
			room, _ := app.Dao().FindFirstRecordByData("DataBlock", "block_id", deviceRecord.GetString("current_room"))
			user, _ := app.Dao().FindRecordById("Users", deviceRecord.GetString("owner"))

			output := map[string]interface{}{
				"device": deviceRecord,
				"room":   room,
				"user":   user,
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
