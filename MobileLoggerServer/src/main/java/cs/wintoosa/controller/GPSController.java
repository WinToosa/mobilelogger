/*
 * To change this template, choose Tools | Templates
 * and open the template in the editor.
 */
package cs.wintoosa.controller;

import cs.wintoosa.domain.Gps;
import cs.wintoosa.domain.Abstractlog;
import cs.wintoosa.service.ILogService;
import java.util.List;
import java.util.logging.Logger;
import javax.validation.Valid;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.http.MediaType;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.RequestMethod;
import org.springframework.web.bind.annotation.ResponseBody;

/**
 *
 * @author vkukkola
 */
@Controller
@RequestMapping(value = "/log/gps")
public class GPSController {
    
    private final static Logger logger = Logger.getLogger(GPSController.class.getName()); 
    
    @Autowired
    ILogService logService;

    @RequestMapping(method= RequestMethod.GET)
    @ResponseBody
    public List<Abstractlog> getLogs() {
        return logService.getAll(Gps.class);
    }
    
    @RequestMapping(method= RequestMethod.PUT, consumes=MediaType.APPLICATION_JSON_VALUE)
    @ResponseBody
    public boolean putGPSLog(@Valid @RequestBody Gps log) {
        return logService.saveLog(log);
    }
    
    @RequestMapping(value="/put", method=RequestMethod.GET)
    public String putDummyLog(){
        Gps log2 = new Gps();
        log2.setAlt(1f);
        log2.setLat(2f);
        log2.setLon(2f);
        log2.setPhoneId("foo");
        log2.setTimestamp(new Long(18));
        logService.saveLog(log2);
        Gps log1 = new Gps();
        log1.setAlt(11f);
        log1.setLat(21f);
        log1.setLon(21f);
        log1.setPhoneId("foo");
        log1.setTimestamp(new Long(55));
        logService.saveLog(log1);
        Gps log = new Gps();
        log.setAlt(111f);
        log.setLat(211f);
        log.setLon(211f);
        log.setPhoneId("foo");
        log.setTimestamp(new Long(32));
        logService.saveLog(log);
        System.out.println("Added dumy log");
        return "redirect:/";
    }
}
