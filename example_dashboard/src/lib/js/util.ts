import { DateTime } from 'luxon';
import humanizeDuration from 'humanize-duration';

export function prettyDate(date: string | Date | DateTime, includeYear = false) {
	if (date == null) return '';

	let d: DateTime;
	if (date instanceof Date) {
		d = DateTime.fromJSDate(date);
	} else if (date instanceof DateTime) {
		d = date;
	} else {
		date = date.replace(' ', 'T');
		d = DateTime.fromISO(date);
	}
	// return DateTime.fromISO(date).toFormat("yyyy-LL-dd hh:mm a ZZZZ");
	const fromNow = DateTime.utc().minus(d.toMillis()).toMillis();
	const yearReplace = includeYear ? '' : ', 2023';
	if (fromNow > 0) {
		return `${d.toLocaleString(DateTime.DATETIME_MED)} (${humanizeDuration(fromNow, {
			round: true,
			largest: 1
		})} ago)`.replaceAll(yearReplace, '');
	} else {
		return `${d.toLocaleString(DateTime.DATETIME_MED)} (in ${humanizeDuration(fromNow, {
			round: true,
			largest: 1
		})})`.replaceAll(yearReplace, '');
	}
}
